using DreamTravel.Domain.Cities;
using SolTechnology.Core.Cache;

namespace DreamTravel.GeolocationDataClients.GoogleApi
{
    public class GoogleHTTPClientCachingDecorator : IGoogleHTTPClient
    {
        private readonly IGoogleHTTPClient _innerClient;
        private readonly IScopedCache<string, City> _scopedCache;
        private readonly ISingletonCache _singletonCache;

        public GoogleHTTPClientCachingDecorator(IGoogleHTTPClient innerClient, IScopedCache<string,City> scopedCache, ISingletonCache singletonCache)
        {
            _innerClient = innerClient;
            _scopedCache = scopedCache;
            _singletonCache = singletonCache;
        }

        public Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities)
        {
            return _innerClient.GetDurationMatrixByTollRoad(listOfCities);
        }

        public Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities)
        {
            return _innerClient.GetDurationMatrixByFreeRoad(listOfCities);
        }

        public Task<TrafficMatrixResponse> GetSegmentDurationMatrixByTraffic(TrafficMatrixRequest request)
        {
            return _innerClient.GetSegmentDurationMatrixByTraffic(request);
        }

        public Task<City> GetLocationOfCity(string cityName)
        {
            return _scopedCache.GetOrAdd(cityName, key => _innerClient.GetLocationOfCity(key));
        }

        public Task<City> GetNameOfCity(City city)
        {
            return _singletonCache.GetOrAdd(city, key => _innerClient.GetNameOfCity(key));
        }
    }
}
