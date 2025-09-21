using System.Text;
using System.Text.Json;
using BackOfficeService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BackOfficeService;

class Program
{
    private static List<TourDetails> _tours = [];
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Back Office Service...");
        
        ConnectionFactory factory = new() { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync();

        CreateChannelOptions channelOptions = new(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(50));
        
        var channel = await connection.CreateChannelAsync(channelOptions);
        
        await channel.ExchangeDeclareAsync("tour.exchange", ExchangeType.Topic, durable: true);
        
        await channel.QueueDeclareAsync("office.queue", durable: true, exclusive: false, autoDelete: false);
        
        await channel.QueueBindAsync("office.queue", "tour.exchange", "tour.*");
        
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            var routingKey = ea.RoutingKey;
            
            Console.WriteLine($"Received Key: {routingKey}");
            HandleRequest(routingKey, message);
            Console.WriteLine("---------------------------");
            
            return Task.CompletedTask;
        };
        
        await channel.BasicConsumeAsync("office.queue", autoAck: true, consumer);
        
        Console.WriteLine("Listening for messages... Press [Enter] to exit");
        Console.ReadLine();
    }

    private static void HandleRequest(string routingKey, string message)
    {
        var tour = JsonSerializer.Deserialize<TourDetails>(message);
        
        if (tour is null)
            return;
        
        switch (routingKey)
        {
            case "tour.booked":
                AddTour(tour);
                break;
            case "tour.cancelled":
                RemoveTour(tour);
                break;
        }
    }

    private static void AddTour(TourDetails tour)
    {
        _tours.Add(tour);

        Console.WriteLine("Added tour to list");
    }

    private static void RemoveTour(TourDetails tour)
    {
        var element = _tours
            .FirstOrDefault( t => 
                t.Name == tour.Name && 
                t.Email == tour.Email && 
                t.PickedTour.Id == tour.PickedTour.Id);
        
        if (element is null)
            return; 
        
        _tours.Remove(element);
        
        Console.WriteLine("Removed tour from list");
    }
}