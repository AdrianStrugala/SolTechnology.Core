using System.Diagnostics;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Infrastructure.Events;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Domain.Events;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.FindCityByName
{
    public class FindCityByNameHandler : IQueryHandler<FindCityByNameQuery, City>
    {
        private readonly IGoogleApiClient _googleApiClient;
        private readonly IMediator _mediator;
        private readonly ILogger<FindCityByNameHandler> _logger;

        public FindCityByNameHandler(
            IGoogleApiClient googleApiClient,
            IMediator mediator,
            ILogger<FindCityByNameHandler> logger)
        {
            _googleApiClient = googleApiClient;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Result<City>> Handle(FindCityByNameQuery query, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await _googleApiClient.GetLocationOfCity(query.Name);
            _logger.LogInformation($"FindCityByName. Http request took: [{stopwatch.ElapsedMilliseconds}]ms");
            stopwatch.Restart();

            //to test cache
            result = await _googleApiClient.GetLocationOfCity(query.Name);
            _logger.LogInformation($"FindCityByName. Cache hit took: [{stopwatch.ElapsedMilliseconds}]ms");

            await _mediator.Publish(new CitySearched { Name = result.Name }, cancellationToken);
            
            stopwatch.Stop();

            return result;
        }
    }
}
