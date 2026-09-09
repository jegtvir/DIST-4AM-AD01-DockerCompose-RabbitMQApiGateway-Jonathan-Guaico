using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Categoria.Api.Models;
using Categoria.Api.Data;
using Categoria.Api.Services;

namespace Categoria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriaController : ControllerBase
    {
        private readonly CategoriaDBContext _dbcontext;
        private readonly IRabbitMQPublisher _publisher;

        public CategoriaController(CategoriaDBContext dBContext, IRabbitMQPublisher publisher)
        {
            _dbcontext = dBContext;
            _publisher = publisher;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<ActionResult<IEnumerable<Categorias>>> GetCategorias()
        {
            var categorias = await _dbcontext.categoria
                .AsNoTracking()
                .ToListAsync();
            return Ok(categorias);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<ActionResult<Categorias>> GetCategoria(int id)
        {
            var categoria = await _dbcontext.categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id_categoria == id);
            if (categoria == null) return NotFound();
            
            return Ok(categoria);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Categorias>> PostCategoria(Categorias categoria)
        {
            _dbcontext.categoria.Add(categoria);
            await _dbcontext.SaveChangesAsync();

            // Publicar el evento de creación a RabbitMQ
            _publisher.PublishCategoryCreated(categoria);

            return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id_categoria }, categoria);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutCategoria(int id, Categorias categoria)
        {
            if(id != categoria.Id_categoria) return BadRequest();
            _dbcontext.Entry(categoria).State = EntityState.Modified;
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _dbcontext.categoria.FindAsync(id);
            if(categoria == null) return NotFound();
            _dbcontext.categoria.Remove(categoria);
            await _dbcontext.SaveChangesAsync();
            return NoContent();
        }
    }
}
