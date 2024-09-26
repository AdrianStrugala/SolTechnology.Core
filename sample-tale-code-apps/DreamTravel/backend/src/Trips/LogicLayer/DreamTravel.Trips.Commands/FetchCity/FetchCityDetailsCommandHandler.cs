using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.Repositories;
using MediatR;

namespace DreamTravel.Trips.Commands.FetchCity
{
    public class FetchCityDetailsCommandHandler : IRequestHandler<FetchCityDetailsCommand>
    {
        private readonly ICityRepository _cityRepository;

        public FetchCityDetailsCommandHandler(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;
        }

        public async Task Handle(FetchCityDetailsCommand request, CancellationToken cancellationToken)
        {
            CityDetails cityDetails = new CityDetails();
            // get city from api
            //store it in db

            await _cityRepository.Add(cityDetails);
        }
    }
}
