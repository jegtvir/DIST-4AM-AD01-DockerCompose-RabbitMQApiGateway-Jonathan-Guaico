using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehiculo.Api.Data;
using Vehiculo.Api.Models;

namespace Vehiculo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehiculoController : ControllerBase
    {
        private readonly VehiculoDBContext _dbcontext;
        public VehiculoController(VehiculoDBContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<ActionResult<IEnumerable<Vehiculos>>> GetVehiculos()
        {
            var vehiculos= await _dbcontext.Vehiculos
                .AsNoTracking()
                .ToListAsync();
            return Ok(vehiculos);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<ActionResult<Vehiculos>> GetVehiculo(int id)
        {
            var vehiculos= await _dbcontext.Vehiculos
                .AsNoTracking()
                .FirstOrDefaultAsync(v=>v.Id_vehiculo==id);
            if (vehiculos == null) return NotFound();
            return Ok(vehiculos);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Vehiculos>> PostVehiculos(Vehiculos vehiculos)
        {
            _dbcontext.Vehiculos.Add(vehiculos);
            await _dbcontext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVehiculo), new {id=vehiculos.Id_vehiculo}, vehiculos);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutVehiculo(int id, Vehiculos vehiculos)
        {
            if (id != vehiculos.Id_vehiculo) return BadRequest();
            _dbcontext.Entry(vehiculos).State = EntityState.Modified;
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteVehiculo(int id)
        {
            var vehiculos= await _dbcontext.Vehiculos.FindAsync(id);
            if (vehiculos == null) return NotFound();
            _dbcontext.Vehiculos.Remove(vehiculos);
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
    }
}
