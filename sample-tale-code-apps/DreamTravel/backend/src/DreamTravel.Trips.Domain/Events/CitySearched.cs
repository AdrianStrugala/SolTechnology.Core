using DreamTravel.Trips.Domain.Cities;
using MediatR;

namespace DreamTravel.Trips.Domain.Events;

public record CitySearched : INotification
{
    public City City { get; set; } = null!;
}