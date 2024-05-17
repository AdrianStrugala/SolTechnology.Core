using System.Diagnostics;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByName
{
    public class FindCityByNameHandler : IQueryHandler<FindCityByNameQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;
        private readonly ILogger<FindCityByNameHandler> _logger;

        public FindCityByNameHandler(IGoogleApiClient googleApiClient, ILogger<FindCityByNameHandler> logger)
        {
            _googleApiClient = googleApiClient;
            _logger = logger;
        }

        public async Task<Result<City>> Handle(FindCityByNameQuery byNameQuery, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await _googleApiClient.GetLocationOfCity(byNameQuery.Name);
            _logger.LogInformation($"FindCityByName. Http request took: [{stopwatch.ElapsedMilliseconds}]ms");
            stopwatch.Restart();

            //to test cache
            result = await _googleApiClient.GetLocationOfCity(byNameQuery.Name);
            _logger.LogInformation($"FindCityByName. Cache hit took: [{stopwatch.ElapsedMilliseconds}]ms");
            stopwatch.Stop();

            return result;
        }
    }
}
