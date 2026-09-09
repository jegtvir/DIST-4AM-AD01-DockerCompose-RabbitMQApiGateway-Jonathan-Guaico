using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Seguridad.Api.Data;
using Seguridad.Api.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Seguridad.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Configurar DbContext con SQL Server
            builder.Services.AddDbContext<SeguridadDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SeguridadConnection")));

            // Registrar servicio JWT
            builder.Services.AddScoped<IJwtService, JwtService>();

            // Configurar autenticación JWT
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

            // Configurar CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Configurar Swagger con Bearer Token
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Seguridad.Api - Microservicio de Autenticación",
                    Version = "v1",
                    Description = "Microservicio para generación y validación de tokens JWT (Administrador / Usuario)"
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

            var app = builder.Build();

            // Habilitar Swagger siempre (Development y Production) para Docker y Azure
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Seguridad API v1");
            });

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Inicializar base de datos y seed de usuarios con reintentos para SQL Server
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var context = services.GetRequiredService<SeguridadDbContext>();
                int retries = 12;
                while (retries > 0)
                {
                    try
                    {
                        logger.LogInformation("Conectando a la base de datos de Seguridad...");
                        context.Database.EnsureCreated();
                        context.SeedInitialData();
                        logger.LogInformation("Base de datos de Seguridad lista y usuarios iniciales precargados (admin / usuario).");
                        break;
                    }
                    catch (Exception ex)
                    {
                        retries--;
                        logger.LogWarning(ex, "No se pudo conectar a la base de datos de Seguridad. Reintentando en 5 segundos... ({Retries} intentos restantes)", retries);
                        Thread.Sleep(5000);
                        if (retries == 0)
                        {
                            logger.LogError("Error fatal: No se pudo establecer la conexión con la base de datos de Seguridad.");
                            throw;
                        }
                    }
                }
            }

            app.Run();
        }
    }
}
