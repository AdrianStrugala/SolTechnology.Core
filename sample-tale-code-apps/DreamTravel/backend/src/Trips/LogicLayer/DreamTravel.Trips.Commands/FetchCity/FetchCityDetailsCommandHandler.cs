using MediatR;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommandHandler : IRequestHandler<FetchCityDetailsCommand>
    {
        public Task Handle(FetchCityDetailsCommand request, CancellationToken cancellationToken)
        {
            // get city from api
            //store it in db
            
            return Task.CompletedTask;
        }
    }
}
