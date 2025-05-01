using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.FetchTraffic
{
    public class FetchTrafficCommand : IRequest<Result>
    {
        public DateTime DepartureTime { get; set; }
    }
}
