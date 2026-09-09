using Microsoft.EntityFrameworkCore;
using Vehiculo.Api.Models;
namespace Vehiculo.Api.Data
{
    public class VehiculoDBContext : DbContext
    {
        public VehiculoDBContext(DbContextOptions<VehiculoDBContext> options) : base(options)
        { 
        }
        public DbSet<Vehiculos> Vehiculos { get; set; }
    }
}
