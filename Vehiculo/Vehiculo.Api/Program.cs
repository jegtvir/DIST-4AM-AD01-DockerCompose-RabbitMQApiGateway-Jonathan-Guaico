
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Vehiculo.Api.Data;
using Vehiculo.Api.Services;

namespace Vehiculo.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddDbContext<VehiculoDBContext>(options => 
                options.UseSqlServer(builder.Configuration.GetConnectionString("VehiculoConnection")));

            // Configuración de JWT
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "JonaPruebaMqRabbit";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            if (keyBytes.Length < 32)
            {
                keyBytes = SHA256.HashData(keyBytes);
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SeguridadApi",
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MicroserviciosDistribuidas",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Vehiculo.Api - Microservicio de Vehículos",
                    Version = "v1",
                    Description = "Microservicio de Vehículos protegido por JWT (Administrador: Total, Usuario: Consulta)"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Autenticación JWT usando el esquema Bearer. Ingrese el token directamente."
                });

                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });

            builder.Services.AddHostedService<RabbitMQConsumer>();

            var app = builder.Build();

            // Habilitar Swagger siempre (Development y Production) para Docker y Azure
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehiculo API v1");
            });

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Create database if not exists (with retries for Docker SQL Server start delay)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var context = services.GetRequiredService<VehiculoDBContext>();
                int retries = 12;
                while (retries > 0)
                {
                    try
                    {
                        logger.LogInformation("Conectando a la base de datos de Vehiculos...");
                        context.Database.EnsureCreated();
                        logger.LogInformation("Base de datos de Vehiculos lista.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        retries--;
                        logger.LogWarning(ex, "No se pudo conectar a la base de datos. Reintentando en 5 segundos... ({Retries} intentos restantes)", retries);
                        System.Threading.Thread.Sleep(5000);
                        if (retries == 0)
                        {
                            logger.LogError("Error fatal: No se pudo establecer la conexion con la base de datos.");
                            throw;
                        }
                    }
                }
            }

            app.Run();
        }
    }
}
