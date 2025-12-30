using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using DreamTravel.GeolocationDataClients.MichelinApi;
using JetBrains.Annotations;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Story;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

[UsedImplicitly]
public class DownloadRoadData(IGoogleHTTPClient googleHTTPClient, IMichelinHTTPClient michelinHTTPClient)
    : Chapter<CalculateBestPathContext>
{
    public override async Task<Result> Read(CalculateBestPathContext calculateBestPathContext)
    {
        var listOfCities = calculateBestPathContext.Cities;
        List<Task> tasks = new List<Task>
        {
            Task.Run(async () => calculateBestPathContext.TollDistances = await googleHTTPClient.GetDurationMatrixByTollRoad(listOfCities)),
            Task.Run(async () => calculateBestPathContext.FreeDistances = await googleHTTPClient.GetDurationMatrixByFreeRoad(listOfCities))
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
                    await michelinHTTPClient.DownloadCostBetweenTwoCities(listOfCities[i1], listOfCities[j1])));
            }
        }

        return tasks;
    }
}