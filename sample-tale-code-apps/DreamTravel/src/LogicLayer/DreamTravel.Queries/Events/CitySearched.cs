using DreamTravel.Domain.Cities;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.Events;

public record CitySearched : IEvent
{
    public City City { get; set; } = null!;
}

