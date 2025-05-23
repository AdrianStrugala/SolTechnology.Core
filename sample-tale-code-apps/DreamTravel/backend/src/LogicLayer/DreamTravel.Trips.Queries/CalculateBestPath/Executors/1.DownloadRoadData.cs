﻿using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.GeolocationDataClients.GoogleApi;
using DreamTravel.Trips.GeolocationDataClients.MichelinApi;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.CQRS.SuperChain;

namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors;

public class DownloadRoadData : IChainStep<CalculateBestPathContext>
{
    private readonly IGoogleApiClient _googleApiClient;
    private readonly IMichelinApiClient _michelinApiClient;

    public DownloadRoadData(IGoogleApiClient googleApiClient, IMichelinApiClient michelinApiClient)
    {
        _googleApiClient = googleApiClient;
        _michelinApiClient = michelinApiClient;
    }

    public async Task<Result> Execute(CalculateBestPathContext calculateBestPathContext)
    {
        var listOfCities = calculateBestPathContext.Cities;
        List<Task> tasks = new List<Task>
        {
            Task.Run(async () => calculateBestPathContext.TollDistances = await _googleApiClient.GetDurationMatrixByTollRoad(listOfCities)),
            Task.Run(async () => calculateBestPathContext.FreeDistances = await _googleApiClient.GetDurationMatrixByFreeRoad(listOfCities))
        };

        tasks.AddRange(DownloadCostMatrix(listOfCities, calculateBestPathContext));

        await Task.WhenAll(tasks);

        return Result.Success();
    }

    private List<Task> DownloadCostMatrix(List<City> listOfCities, CalculateBestPathContext calculateBestPathContext)
    {
        List<Task> tasks = new List<Task>();

        for (int i = 0; i < listOfCities.Count; i++)
        {
            for (int j = 0; j < listOfCities.Count; j++)
            {
                int iterator = j + i * listOfCities.Count;

                var i1 = i;
                var j1 = j;
                tasks.Add(Task.Run(async () => (calculateBestPathContext.Costs[iterator], calculateBestPathContext.VinietaCosts[iterator]) =
                    await _michelinApiClient.DownloadCostBetweenTwoCities(listOfCities[i1], listOfCities[j1])));
            }
        }

        return tasks;
    }
}