namespace DreamTravel.Trips.Queries.CalculateBestPath.Executors;

public interface IFindProfitablePath
{
    void Execute(CalculateBestPathContext calculateBestPathContext, int noOfCities);
}

public class FindProfitablePath : IFindProfitablePath
{
    private static double FuelPrice { get; } = 1.26;
    private static double RoadVelocity { get; } = 70;
    private static double HighwayVelocity { get; } = 120;
    private static double RoadCombustion { get; } = 0.06; //per km

    public void Execute(CalculateBestPathContext calculateBestPathContext, int noOfCities)
    {
        Parallel.For(0, noOfCities, i =>
        {
            Parallel.For(0, noOfCities, j =>
            {
                if (i != j)
                {
                    int iterator = j + i * noOfCities;

                    //if toll takes more time than regular -> pretend it does not exist
                    if (calculateBestPathContext.TollDistances[iterator] > calculateBestPathContext.FreeDistances[iterator])
                    {
                        calculateBestPathContext.TollDistances[iterator] = calculateBestPathContext.FreeDistances[iterator];
                        calculateBestPathContext.Costs[iterator] = 0;
                    }

                    if (IsTollRoadProfitable(calculateBestPathContext, iterator))
                    {
                        calculateBestPathContext.OptimalDistances[iterator] = calculateBestPathContext.TollDistances[iterator];
                        calculateBestPathContext.OptimalCosts[iterator] = calculateBestPathContext.Costs[iterator];
                    }
                    else
                    {
                        calculateBestPathContext.OptimalDistances[iterator] = calculateBestPathContext.FreeDistances[iterator];
                        calculateBestPathContext.OptimalCosts[iterator] = 0;
                    }
                }
            });
        });
    }


    private static bool IsTollRoadProfitable(CalculateBestPathContext calculateBestPathContext, int iterator)
    {
        //roads using vinieta are never profitable
        if (calculateBestPathContext.VinietaCosts[iterator] != 0)
        {
            return false;
        }

        // C_G=s×combustion×fuel price [€] = v x t x combustion x fuel 
        double gasolineCostFree =
            calculateBestPathContext.FreeDistances[iterator] /
            3600.0 * RoadVelocity * RoadCombustion * FuelPrice;

        double gasolineCostToll =
            calculateBestPathContext.TollDistances[iterator] /
            3600.0 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;

        //toll goal = (cost of gasoline + cost of toll fee) * time of toll
        double cost = (gasolineCostToll + calculateBestPathContext.Costs[iterator]);
        double time = (calculateBestPathContext.TollDistances[iterator] / 3600.0);
        double importance = (calculateBestPathContext.TollDistances[iterator] * 1.0 /
            calculateBestPathContext.FreeDistances[iterator] * 1.0);
        var tollGoal = cost * time * importance;
        var freeGoal = gasolineCostFree * (calculateBestPathContext.FreeDistances[iterator] / 3600.0);


        calculateBestPathContext.Goals[iterator] = tollGoal;

        return freeGoal > tollGoal;
    }
}