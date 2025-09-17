using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BackOfficeService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Back Office Service...");
        
        ConnectionFactory factory = new() { HostName = "localhost" };
        IConnection connection = await factory.CreateConnectionAsync();

        CreateChannelOptions channelOptions = new(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(50));
        
        IChannel channel = await connection.CreateChannelAsync(channelOptions);
        
        await channel.ExchangeDeclareAsync("tour.exchange", ExchangeType.Topic, durable: true);
        
        await channel.QueueDeclareAsync("office.queue", durable: true, exclusive: false, autoDelete: false);
        
        await channel.QueueBindAsync("office.queue", "tour.exchange", "tour.*");
        
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            string message = Encoding.UTF8.GetString(ea.Body.ToArray());
            string routingKey = ea.RoutingKey;
            
            Console.WriteLine($"Routing Key: {routingKey}");
            Console.WriteLine($"Message: {message}");
            Console.WriteLine("---");
            
            return Task.CompletedTask;
        };
        
        await channel.BasicConsumeAsync("office.queue", autoAck: true, consumer);
        
        Console.WriteLine("Listening for messages... Press [Enter] to exit");
        Console.ReadLine();
    }
}