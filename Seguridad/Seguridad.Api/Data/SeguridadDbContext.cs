using Microsoft.EntityFrameworkCore;
using Seguridad.Api.Models;

namespace Seguridad.Api.Data
{
    public class SeguridadDbContext : DbContext
    {
        public SeguridadDbContext(DbContextOptions<SeguridadDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Correo).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(20);
            });
        }

        public void SeedInitialData()
        {
            if (!Usuarios.Any())
            {
                var admin = new Usuario
                {
                    NombreCompleto = "Administrador del Sistema",
                    Username = "admin",
                    Correo = "admin@distribuidas.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                    Rol = "Administrador",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var user = new Usuario
                {
                    NombreCompleto = "Usuario Regular",
                    Username = "usuario",
                    Correo = "usuario@distribuidas.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123*"),
                    Rol = "Usuario",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                Usuarios.AddRange(admin, user);
                SaveChanges();
            }
        }
    }
}
