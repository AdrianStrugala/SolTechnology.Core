using System.Diagnostics;
using DreamTravel.DomainServices.CityDomain;
using DreamTravel.Infrastructure.Events;
using DreamTravel.Domain.Cities;
using DreamTravel.Domain.Events;
using DreamTravel.GeolocationDataClients.GoogleApi;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.FindCityByName
{
    public class FindCityByNameHandler(
        ICityDomainService cityDomainService,
        IHangfireNotificationPublisher hangfireNotificationPublisher,
        ILogger<FindCityByNameHandler> logger)
        : IQueryHandler<FindCityByNameQuery, City>
    {
        public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await cityDomainService.Get(query.Name);
            logger.LogInformation($"FindCityByName. Http request took: [{stopwatch.ElapsedMilliseconds}]ms");
            stopwatch.Restart();

            //to test cache
            result = await cityDomainService.Get(query.Name);
            logger.LogInformation($"FindCityByName. Cache hit took: [{stopwatch.ElapsedMilliseconds}]ms");
            
            hangfireNotificationPublisher.Publish(new CitySearched { City = result });
            
            stopwatch.Stop();

            return result;
        }
    }
}
