using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AdminService;

class Program
{
    private static List<object> _log = [];
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Admin Service Service...");
        
        ConnectionFactory factory = new() { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();
        
        var channel = await connection.CreateChannelAsync();
        
        await channel.ExchangeDeclareAsync("admin.exchange", ExchangeType.Direct, durable: false);
        
        await channel.QueueDeclareAsync("admin.invalid", durable: false, exclusive: false, autoDelete: false);
        await channel.QueueDeclareAsync("email.queue.DLX.queue", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueDeclareAsync("office.queue.DLX.queue", durable: true, exclusive: false, autoDelete: false);

        await channel.QueueBindAsync("admin.invalid", "admin.exchange", "admin.invalid");
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            var routingKey = ea.RoutingKey;

            var deserialized = JsonSerializer.Deserialize<object>(message);
            
            Console.WriteLine($"Received Invalid Message");
            
            if (deserialized is not null)
                _log.Add(deserialized);
    
            Console.WriteLine("---------------------------");
            
            return Task.CompletedTask;
        };
        
        await channel.BasicConsumeAsync("admin.invalid", autoAck: true, consumer);
        await channel.BasicConsumeAsync("email.queue.DLX.queue", autoAck: true, consumer);
        await channel.BasicConsumeAsync("office.queue.DLX.queue", autoAck: true, consumer);

        Console.WriteLine("Listening for messages... Press [Enter] to view list");
        Console.ReadLine();

        foreach (var item in _log)
        {
            Console.WriteLine(item);
        }

        Console.ReadLine();
    }
}