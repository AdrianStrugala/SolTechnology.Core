using System.Diagnostics;
using DreamTravel.Infrastructure.Events;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.Events;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByName
{
    public class FindCityByNameHandler : IQueryHandler<FindCityByNameQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;
        private readonly IHangfireNotificationPublisher _hangfireNotificationPublisher;
        private readonly ILogger<FindCityByNameHandler> _logger;

        public FindCityByNameHandler(
            IGoogleApiClient googleApiClient,
            IHangfireNotificationPublisher hangfireNotificationPublisher,
            ILogger<FindCityByNameHandler> logger)
        {
            _googleApiClient = googleApiClient;
            _hangfireNotificationPublisher = hangfireNotificationPublisher;
            _logger = logger;
        }

        public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //TODO: try get from database, fallback to google api
            
            var result = await _googleApiClient.GetLocationOfCity(query.Name);
            _logger.LogInformation($"FindCityByName. Http request took: [{stopwatch.ElapsedMilliseconds}]ms");
            stopwatch.Restart();

            //to test cache
            result = await _googleApiClient.GetLocationOfCity(query.Name);
            _logger.LogInformation($"FindCityByName. Cache hit took: [{stopwatch.ElapsedMilliseconds}]ms");

            //TODO: pass isInDb flag to notification
            _hangfireNotificationPublisher.Publish(new CitySearched { Name = result.Name });
            
            stopwatch.Stop();

            return result;
        }
    }
}
