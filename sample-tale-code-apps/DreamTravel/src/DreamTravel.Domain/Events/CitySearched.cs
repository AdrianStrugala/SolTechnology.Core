using DreamTravel.Domain.Cities;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Domain.Events;

public record CitySearched : INotification
{
    public City City { get; set; } = null!;
}

