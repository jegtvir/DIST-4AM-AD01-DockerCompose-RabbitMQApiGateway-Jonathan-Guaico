using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Vehiculo.Api.Data;
using Vehiculo.Api.Models;

namespace Vehiculo.Api.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQConsumer(IConfiguration configuration, ILogger<RabbitMQConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            int retries = 12;
            while (retries > 0)
            {
                try
                {
                    var factory = new ConnectionFactory()
                    {
                        HostName = _configuration["RabbitMQ:HostName"],
                        UserName = _configuration["RabbitMQ:UserName"],
                        Password = _configuration["RabbitMQ:Password"],
                        Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.QueueDeclare(queue: "categoria_created_queue",
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    _logger.LogInformation("Conexión con RabbitMQ establecida con éxito en Vehiculo.Api.");
                    break;
                }
                catch (Exception ex)
                {
                    retries--;
                    _logger.LogWarning("No se pudo conectar a RabbitMQ en Vehiculo.Api. Reintentando en 5 segundos... ({Retries} intentos restantes)", retries);
                    System.Threading.Thread.Sleep(5000);
                    if (retries == 0)
                    {
                        _logger.LogError(ex, "Error fatal: No se pudo conectar a RabbitMQ tras varios intentos en Vehiculo.Api.");
                    }
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogWarning("El canal de RabbitMQ no está inicializado. No se consumirán mensajes en Vehiculo.Api.");
                return Task.CompletedTask;
            }

            stoppingToken.Register(() => {
                _logger.LogInformation("Deteniendo el consumidor de RabbitMQ...");
                _channel?.Close();
                _connection?.Close();
            });

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Vehiculo.Api recibió evento de RabbitMQ: {Message}", message);

                try
                {
                    using var document = JsonDocument.Parse(message);
                    int idCategoria = 0;
                    bool idFound = false;

                    if (document.RootElement.TryGetProperty("Id_categoria", out var idProp) ||
                        document.RootElement.TryGetProperty("id_categoria", out idProp))
                    {
                        idCategoria = idProp.GetInt32();
                        idFound = true;
                    }

                    if (idFound)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<VehiculoDBContext>();

                        var nuevoVehiculo = new Vehiculos
                        {
                            Id_categoria = idCategoria,
                            marca_vehiculo = "prueba_rabbitmq",
                            modelo_vehiculo = "prueba_rabbitmq",
                            precio_vehiculo = 15000,
                            stock_vehiculo = 5,
                            estado_vehiculo = true
                        };

                        dbContext.Vehiculos.Add(nuevoVehiculo);
                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("Vehículo creado automáticamente en la base de datos para la categoría ID: {IdCategoria}", idCategoria);
                    }
                    else
                    {
                        _logger.LogWarning("No se pudo extraer 'Id_categoria' del mensaje.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar el mensaje y crear el vehículo.");
                }

                // Acknowledge del mensaje
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            _channel.BasicConsume(queue: "categoria_created_queue",
                                 autoAck: false,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
