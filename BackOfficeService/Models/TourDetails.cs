namespace BackOfficeService.Models;

public class TourDetails
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } =  string.Empty;
    
    public required Tour PickedTour { get; set; }
}

public class Tour
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}