using MediatR;

namespace DreamTravel.Trips.Domain.Events;

public record CitySearched : INotification
{
    public string Name { get; set; } = null!;
    
    public bool IsInDb { get; set; }
}