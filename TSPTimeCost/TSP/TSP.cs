using System.Collections.Generic;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    abstract class TSP
    {
        private static readonly int NoOfCities = Cities.Instance.ListOfCities.Count;

        //Goal parameters
        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        public abstract void SolveTSP();

        protected void CalculateDistance()
        {
            BestPath.Instance.Distance = 0;
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Instance.Distance += BestPath.Instance.DistancesInOrder[i];
            }
        }

        protected void UpdateCost()
        {
            BestPath.Instance.Cost = 0;

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                if (BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForTollRoads.Instance.Distances[BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]])
                {
                    BestPath.Instance.Cost += CostMatrix.Instance.Value[
                        BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]];
                }

                BestPath.Instance.Goal[i] =
                    DistanceMatrixEvaluated.Instance.Goals[
                        BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]];
            }
        }

        protected void UpdateGoal()
        {
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Instance.Goal[i] =
                    DistanceMatrixEvaluated.Instance.Goals[
                        BestPath.Instance.Order[i] * NoOfCities + BestPath.Instance.Order[i + 1]];
            }
        }

        public double CalculateDistanceInPath(int[] path, IDistanceMatrix distanceMatrix)
        {
            double result = 0;

            for (int i = 0; i < path.Length - 1; i++)
            {
                result += distanceMatrix.GetInstance().Distances[path[i] * path.Length + path[i + 1]];
            }
            return result;
        }

        //G=  ΔC/ΔT??? C*T
        protected List<TimeDifferenceAndCost> CalculateGoal()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            List<TimeDifferenceAndCost> goalList = new List<TimeDifferenceAndCost>();

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                City origin = cities[BestPath.Instance.Order[i]];
                City destination = cities[BestPath.Instance.Order[i + 1]];
                var indexOrigin = cities.IndexOf(origin);
                var indexDestination = cities.IndexOf(destination);

                TimeDifferenceAndCost goalItem = InitializeGoalItem(i, indexOrigin, indexDestination);

                if (IsFreeRoad(i, indexOrigin, indexDestination))
                {
                    goalItem = CalculateGoalForFreeRoad(indexOrigin, indexDestination, goalItem);
                }

                // C_G = s × combustion × fuel price [€] 
                else
                {
                    goalItem = CalculateGoalForTollRoad(indexOrigin, indexDestination, goalItem);
                }

                BestPath.Instance.Goal[i] = goalItem.Goal;

                goalList.Add(goalItem);
            }

            return goalList;
        }

        private static TimeDifferenceAndCost InitializeGoalItem(int i, int indexOrigin, int indexDestination)
        {
            TimeDifferenceAndCost goalItem =
                new TimeDifferenceAndCost
                {
                    FeeCost = CostMatrix.Instance.Value[indexOrigin + NoOfCities * indexDestination],
                    Index = i,
                    TimeDifference =
                        DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination] -
                        DistanceMatrixForTollRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination],
                    GasolineCostFree =
                        DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination] /
                        3600 * RoadVelocity * RoadCombustion * FuelPrice,
                    // C_G=s×combustion×fuel price [€]
                    GasolineCostToll =
                        DistanceMatrixForTollRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination] /
                        3600 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice
                };
            return goalItem;
        }

        private static bool IsFreeRoad(int i, int indexOrigin, int indexDestination)
        {
            return BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination];
        }

        private static TimeDifferenceAndCost CalculateGoalForTollRoad(int indexOrigin, int indexDestination, TimeDifferenceAndCost goalItem)
        {
            goalItem.Goal = goalItem.GasolineCostToll *
                            (DistanceMatrixForTollRoads.Instance.Distances[
                                 indexOrigin + NoOfCities * indexDestination] / 3600);

            return goalItem;
        }

        protected static TimeDifferenceAndCost CalculateGoalForFreeRoad(int indexOrigin, int indexDestination, TimeDifferenceAndCost goalItem)
        {
            goalItem.Goal = goalItem.GasolineCostFree *
                            (DistanceMatrixForFreeRoads.Instance.Distances[
                                 indexOrigin + NoOfCities * indexDestination] / 3600);

            return goalItem;
        }
    }
}
