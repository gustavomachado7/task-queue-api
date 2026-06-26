using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Infrastructure.Messaging
{
    public class RabbitJobPublisher : IJobPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "taskqueue.jobs";

        public RabbitJobPublisher(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).GetAwaiter().GetResult();
        }

        public async Task PublishAsync(JobRecord job)
        {
            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));

            // Persistent: true garante que a mensagem não é perdida se o RabbitMQ reiniciar
            var properties = new BasicProperties { Persistent = true };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                basicProperties: properties,
                body: messageBody
            );
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }
    }
}