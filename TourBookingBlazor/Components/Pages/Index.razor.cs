using Microsoft.AspNetCore.Components;
using TourBookingBlazor.Features.RabbitMQ;
using TourBookingBlazor.Models;

namespace TourBookingBlazor.Components.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    public required RabbitService Rabbit { get; set; }
    
    private IEnumerable<TourView> Tours { get; set; } = new List<TourView>
    {
        new()
        {
            Id = 1,
            Name = "Norway"
        },
        new()
        {
            Id = 2,
            Name = "Sweden"
        }
    };

    private bool _isBooking = true;
    private TourView? _selectedTour = null;
    private string _typedName = string.Empty;
    private string _typedEmail = string.Empty;

    private async Task HandleSubmit()
    {
        if (_isBooking)
        {
            await PostBooking();
        }
        else
        {
            await CancelBooking();
        }
    }

    private void HandleReset()
    {
        _typedName = string.Empty;
        _typedEmail = string.Empty;
        _selectedTour = null;
        _isBooking = true;
    }

    private async Task PostBooking()
    {
        await Rabbit.PublishToQueue("tour.booked", new
        {
            Name = _typedName,
            Email = _typedEmail,
            PickedTour = _selectedTour
        });
    }

    private async Task CancelBooking()
    {
        await Rabbit.PublishToQueue("tour.cancelled", new
        {
            Name = _typedName,
            Email = _typedEmail,
            PickedTour = _selectedTour
        });
    }
}