using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationData;

namespace DreamTravel.Features.FindLocationOfCity
{
    public class FindLocationOfCity : IFindLocationOfCity
    {
        private readonly ICityRepository _cityRepository;

        public FindLocationOfCity(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;

        }

        public async Task<City> Execute(string cityName)
        {
            var result = await _cityRepository.GetLocation(cityName);

            return result;
        }
    }
}
