using MediatR;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommand : IRequest
    {
        public string Name { get; set; } = null!;
    }
}
