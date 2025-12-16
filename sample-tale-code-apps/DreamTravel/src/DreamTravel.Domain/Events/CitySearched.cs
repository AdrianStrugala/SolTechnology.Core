using DreamTravel.Domain.Cities;
using MediatR;

namespace DreamTravel.Domain.Events;

public record CitySearched : INotification
{
    public City City { get; set; } = null!;
}