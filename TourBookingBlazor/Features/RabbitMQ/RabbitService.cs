using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace TourBookingBlazor.Features.RabbitMQ;

public class RabbitService : IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isDisposed = false;

    private const string TourExchange = "tour.exchange";

    public async Task InitializeAsync()
    {
        if (_connection != null && _connection.IsOpen)
            return;

        try
        {
            var factory = new ConnectionFactory()
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost",
                Port = 5672,
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            
            await _channel.ExchangeDeclareAsync(TourExchange, ExchangeType.Topic, durable: true);
        }
        catch (Exception)
        {
            Console.WriteLine("Couldn't establish connection");
            throw;
        }
    }

    public async Task PublishToQueue(string routingKey, object messageBody)
    {
        await InitializeAsync();

        if (_channel is null || !_channel.IsOpen)
        {
            throw new InvalidOperationException("Channel is not available");
        }

        try
        {
            var json = JsonSerializer.Serialize(messageBody);

            var encoded = Encoding.UTF8.GetBytes(json);

            BasicProperties props = new();

            props.DeliveryMode = DeliveryModes.Persistent;
            
            await _channel.BasicPublishAsync(
                exchange: TourExchange,
                routingKey: routingKey,
                body: encoded,
                basicProperties: props,
                mandatory: false);

            Console.WriteLine($"Published message to {routingKey}: {json}");
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to publish message");
            throw;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _channel?.CloseAsync();
        _channel?.Dispose();

        _connection?.CloseAsync();
        _connection?.Dispose();

        _isDisposed = true;
    }
}