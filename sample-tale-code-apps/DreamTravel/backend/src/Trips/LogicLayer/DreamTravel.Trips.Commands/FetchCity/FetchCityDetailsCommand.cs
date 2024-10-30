using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommand : IRequest<Result>
    {
        public string Name { get; set; } = null!;
    }
}
