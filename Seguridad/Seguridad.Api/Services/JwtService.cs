using Microsoft.IdentityModel.Tokens;
using Seguridad.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Seguridad.Api.Services
{
    public interface IJwtService
    {
        (string Token, DateTime Expiration) GenerateToken(Usuario usuario);
        SymmetricSecurityKey GetSecurityKey();
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SymmetricSecurityKey GetSecurityKey()
        {
            var rawKey = _configuration["Jwt:Key"] ?? "JonaPruebaMqRabbit";
            var keyBytes = Encoding.UTF8.GetBytes(rawKey);

            // Asegurar un mínimo de 256 bits (32 bytes) para HMAC-SHA256
            if (keyBytes.Length < 32)
            {
                keyBytes = SHA256.HashData(keyBytes);
            }

            return new SymmetricSecurityKey(keyBytes);
        }

        public (string Token, DateTime Expiration) GenerateToken(Usuario usuario)
        {
            var key = GetSecurityKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("role", usuario.Rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("nombreCompleto", usuario.NombreCompleto)
            };

            var issuer = _configuration["Jwt:Issuer"] ?? "SeguridadApi";
            var audience = _configuration["Jwt:Audience"] ?? "MicroserviciosDistribuidas";

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(tokenDescriptor);

            return (tokenString, expiration);
        }
    }
}
