using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EmailService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Email Service...");
        
        ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
        IConnection connection = await factory.CreateConnectionAsync();
        IChannel channel = await connection.CreateChannelAsync();
        
        await channel.ExchangeDeclareAsync("tour.exchange", ExchangeType.Topic, durable: true);
        
        await channel.QueueDeclareAsync("email.queue", durable: true, exclusive: false, autoDelete: false);
        
        await channel.QueueBindAsync("email.queue", "tour.exchange", "tour.booked");
        
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
        
        await channel.BasicConsumeAsync("email.queue", autoAck: true, consumer);
        
        Console.WriteLine("Listening for messages... Press [Enter] to exit");
        Console.ReadLine();
    }
}