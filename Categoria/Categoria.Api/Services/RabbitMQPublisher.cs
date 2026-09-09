using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Categoria.Api.Services
{
    public interface IRabbitMQPublisher
    {
        void PublishCategoryCreated(object category);
    }

    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void PublishCategoryCreated(object category)
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

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "categoria_created_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var messageJson = JsonSerializer.Serialize(category);
                var body = Encoding.UTF8.GetBytes(messageJson);

                channel.BasicPublish(exchange: "",
                                     routingKey: "categoria_created_queue",
                                     basicProperties: null,
                                     body: body);

                _logger.LogInformation("Mensaje de Categoría Creada enviado a RabbitMQ: {Message}", messageJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje a RabbitMQ");
            }
        }
    }
}
