using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQService;

namespace EmailService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Email Service...");

        RabbitService rabbitService = new RabbitService("localhost");
        ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
        IConnection connection = await factory.CreateConnectionAsync();
        IChannel channel = await connection.CreateChannelAsync();

        await rabbitService.SetupQueue("email.queue", "tour.exchange", "tour.booked", true, true);

        await channel.ExchangeDeclareAsync("admin.exchange", ExchangeType.Direct, durable: false);
        
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                string message = Encoding.UTF8.GetString(ea.Body.ToArray());
                string routingKey = ea.RoutingKey;
            
                Console.WriteLine($"Routing Key: {routingKey}");
                Console.WriteLine($"Message: {message}");
                Console.WriteLine("---");
            }
            catch (Exception ex)
            {
                var body = new
                {
                    TimeStamp = DateTime.Now,
                    Service = "AdminService",
                    ea.RoutingKey,
                    ea.Body,
                };

                var json = JsonSerializer.Serialize(body);
                
                await channel.BasicPublishAsync(
                    exchange: "admin.exchange",
                    routingKey: "admin.invalid",
                    body: Encoding.UTF8.GetBytes(json),
                    mandatory: false);

                Console.WriteLine("Invalid message logged");
                Console.WriteLine("---------------------------");
            }
        };
        
        await channel.BasicConsumeAsync("email.queue", autoAck: true, consumer);
        
        Console.WriteLine("Listening for messages... Press [Enter] to exit");
        Console.ReadLine();
    }
}