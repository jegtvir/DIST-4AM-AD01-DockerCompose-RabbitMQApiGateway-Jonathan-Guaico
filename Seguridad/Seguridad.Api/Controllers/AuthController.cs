    using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Seguridad.Api.Data;
using Seguridad.Api.Models;
using Seguridad.Api.Services;
using System.Security.Claims;

namespace Seguridad.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SeguridadDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(SeguridadDbContext context, IJwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower() || u.Correo.ToLower() == model.Username.ToLower());

            if (usuario == null)
            {
                _logger.LogWarning("Intento de inicio de sesión fallido para usuario: {Username}", model.Username);
                return Unauthorized(new AuthResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas: el usuario no existe"
                });
            }

            if (!usuario.Activo)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Exito = false,
                    Mensaje = "El usuario se encuentra inactivo en el sistema"
                });
            }

            bool passwordValido = BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash);
            if (!passwordValido)
            {
                _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", model.Username);
                return Unauthorized(new AuthResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas: contraseña no válida"
                });
            }

            var (token, expiracion) = _jwtService.GenerateToken(usuario);

            _logger.LogInformation("Usuario {Username} con rol {Rol} autenticado exitosamente.", usuario.Username, usuario.Rol);

            return Ok(new AuthResponseDto
            {
                Exito = true,
                Mensaje = "Inicio de sesión exitoso",
                Token = token,
                TokenType = "Bearer",
                Username = usuario.Username,
                Rol = usuario.Rol,
                Expiracion = expiracion
            });
        }

        [HttpPost("registro")]
        public async Task<ActionResult<AuthResponseDto>> Registro([FromBody] RegistroDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existeUsuario = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == model.Username.ToLower() || u.Correo.ToLower() == model.Correo.ToLower());

            if (existeUsuario)
            {
                return BadRequest(new AuthResponseDto
                {
                    Exito = false,
                    Mensaje = "El nombre de usuario o correo ya se encuentra registrado"
                });
            }

            // Normalizar el rol: sólo se permite 'Administrador' o 'Usuario'
            string rolAsignado = "Usuario";
            if (!string.IsNullOrWhiteSpace(model.Rol) && model.Rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                rolAsignado = "Administrador";
            }

            var nuevoUsuario = new Usuario
            {
                NombreCompleto = model.NombreCompleto,
                Username = model.Username.Trim(),
                Correo = model.Correo.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Rol = rolAsignado,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var (token, expiracion) = _jwtService.GenerateToken(nuevoUsuario);

            _logger.LogInformation("Nuevo usuario registrado: {Username} con rol: {Rol}", nuevoUsuario.Username, nuevoUsuario.Rol);

            return Ok(new AuthResponseDto
            {
                Exito = true,
                Mensaje = "Usuario registrado exitosamente",
                Token = token,
                TokenType = "Bearer",
                Username = nuevoUsuario.Username,
                Rol = nuevoUsuario.Rol,
                Expiracion = expiracion
            });
        }

        [Authorize]
        [HttpGet("perfil")]
        public async Task<IActionResult> GetPerfil()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name;
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (usuario == null)
            {
                return NotFound(new { Mensaje = "Usuario no encontrado" });
            }

            return Ok(new
            {
                usuario.Id,
                usuario.NombreCompleto,
                usuario.Username,
                usuario.Correo,
                Rol = rol ?? usuario.Rol,
                usuario.FechaCreacion
            });
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.NombreCompleto,
                    u.Username,
                    u.Correo,
                    u.Rol,
                    u.Activo,
                    u.FechaCreacion
                })
                .ToListAsync();

            return Ok(usuarios);
        }
    }
}
