using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.FetchCityDetails
{
    public class FetchCityDetailsCommand : IRequest<Result>
    {
        public string Name { get; set; } = null!;
    }
}
