using System.Collections.Generic;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    abstract class TSP
    {
        //Goal parameters

        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        public abstract void SolveTSP();

        public double CalculateDistanceInPath(int[] path, IDistanceMatrix distanceMatrix)
        {
            double result = 0;

            for (int i = 0; i < path.Length - 1; i++)
            {
                result += distanceMatrix.GetInstance().Value[path[i] * path.Length + path[i + 1]];
            }
            return result;
        }

        //G=  ΔC/ΔT
        protected List<TimeDifferenceAndCost> CalculateGoal()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            List<TimeDifferenceAndCost> goalList = new List<TimeDifferenceAndCost>();

            for (int i = 0; i < cities.Count - 1; i++)
            {
                City origin = cities[BestPath.Instance.Order[i]];
                City destination = cities[BestPath.Instance.Order[i + 1]];
                var indexOrigin = cities.IndexOf(origin);
                var indexDestination = cities.IndexOf(destination);

                TimeDifferenceAndCost goalItem = InitializeGoalItem(cities, i, indexOrigin, indexDestination);

                if (IsFreeRoad(cities, i, indexOrigin, indexDestination))
                {
                    goalItem = CalculateGoalForFreeRoad(cities, indexOrigin, indexDestination, goalItem);
                }

                // C_G = s × combustion × fuel price [€] 
                else
                {
                    goalItem = CalculateGoalForTollRoad(cities, indexOrigin, indexDestination, goalItem);
                }

                BestPath.Instance.Goal[i] = goalItem.Goal;

                goalList.Add(goalItem);
            }

            return goalList;
        }

        private static TimeDifferenceAndCost InitializeGoalItem(List<City> cities, int i, int indexOrigin, int indexDestination)
        {
            TimeDifferenceAndCost goalItem =
                new TimeDifferenceAndCost
                {
                    FeeCost = CostMatrix.Instance.Value[indexOrigin + cities.Count * indexDestination],
                    Index = i,
                    TimeDifference =
                        DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] -
                        DistanceMatrixForTollRoads.Instance.Value[indexOrigin + cities.Count * indexDestination],
                    GasolineCostFree =
                        DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] /
                        3600 * RoadVelocity * RoadCombustion * FuelPrice,
                    // C_G=s×combustion×fuel price [€]
                    GasolineCostToll =
                        DistanceMatrixForTollRoads.Instance.Value[indexOrigin + cities.Count * indexDestination] /
                        3600 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice
                };
            return goalItem;
        }

        private static bool IsFreeRoad(List<City> cities, int i, int indexOrigin, int indexDestination)
        {
            return BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Value[indexOrigin + cities.Count * indexDestination];
        }

        private static TimeDifferenceAndCost CalculateGoalForTollRoad(List<City> cities, int indexOrigin, int indexDestination, TimeDifferenceAndCost goalItem)
        {
            goalItem.Goal = goalItem.GasolineCostToll *
                            (DistanceMatrixForTollRoads.Instance.Value[
                                 indexOrigin + cities.Count * indexDestination] / 3600);

            return goalItem;
        }

        protected static TimeDifferenceAndCost CalculateGoalForFreeRoad(List<City> cities, int indexOrigin, int indexDestination, TimeDifferenceAndCost goalItem)
        {
            goalItem.Goal = goalItem.GasolineCostFree *
                            (DistanceMatrixForFreeRoads.Instance.Value[
                                 indexOrigin + cities.Count * indexDestination] / 3600);

            return goalItem;
        }
    }
}
