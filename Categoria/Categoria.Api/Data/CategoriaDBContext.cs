using Microsoft.EntityFrameworkCore;
using Categoria.Api.Models;

namespace Categoria.Api.Data
{
    public class CategoriaDBContext : DbContext
    {
        public CategoriaDBContext(DbContextOptions<CategoriaDBContext> options) : base(options)
        {

        }
        public DbSet<Categorias> categoria { get; set; }
    }
}
