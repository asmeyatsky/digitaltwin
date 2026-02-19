using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqEventBus> _logger;
        private const string ExchangeName = "digitaltwin.events";
        private readonly ConcurrentDictionary<string, string> _consumerQueues = new();

        public RabbitMqEventBus(ILogger<RabbitMqEventBus> logger)
        {
            _logger = logger;

            var connectionString = Environment.GetEnvironmentVariable("RabbitMQ__ConnectionString")
                ?? "amqp://guest:guest@localhost:5672/";

            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

            _logger.LogInformation("RabbitMQ event bus connected to {Exchange}", ExchangeName);
        }

        public Task PublishAsync<T>(string eventName, T data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: eventName,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug("Published event {EventName}", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventName}", eventName);
            }

            return Task.CompletedTask;
        }

        public void Subscribe<T>(string eventName, Func<T, Task> handler)
        {
            var queueName = $"digitaltwin.{eventName}";
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, ExchangeName, eventName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var data = JsonSerializer.Deserialize<T>(json);
                    if (data != null)
                    {
                        await handler(data);
                    }
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling event {EventName}", eventName);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queueName, autoAck: false, consumer: consumer);
            _consumerQueues[eventName] = queueName;

            _logger.LogInformation("Subscribed to event {EventName} on queue {Queue}", eventName, queueName);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
