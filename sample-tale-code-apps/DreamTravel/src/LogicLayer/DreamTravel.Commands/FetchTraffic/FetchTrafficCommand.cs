using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Commands.FetchTraffic
{
    public class FetchTrafficCommand : IRequest<Result>
    {
        public DateTime DepartureTime { get; set; }
    }
}
