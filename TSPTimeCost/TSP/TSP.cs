using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    abstract class TSP
    {
        protected static readonly IDistanceMatrix TollMatrix = DistanceMatrixForTollRoads.Instance;
        protected static readonly IDistanceMatrix FreeMatrix = DistanceMatrixForFreeRoads.Instance;
        protected static readonly IDistanceMatrix EvaluatedMatrix = DistanceMatrixEvaluated.Instance;
        protected static readonly BestPath BestPath = BestPath.Instance;

        protected static readonly int NoOfCities = Cities.Instance.ListOfCities.Count;

        public abstract void SolveTSP();

        protected static void RewriteFoundPathToBestPath(List<int> path)
        {
            for (int i = 0; i < NoOfCities; i++)
            {
                BestPath.Order[i] = path[i];
            }
        }

        protected static void RewriteFoundPathToBestPath(int[] path)
        {
            for (int i = 0; i < NoOfCities; i++)
            {
                BestPath.Order[i] = path[i];
            }
        }

        protected static void RewriteFoundPathToBestPath(List<FanAndStar> fansAndStars)
        {
            for (int i = 1; i <= NoOfCities - 1; i++)
            {
                BestPath.Order[i] = fansAndStars.First(element => element.Fan == BestPath.Order[i - 1]).Star;
            }
        }

        protected static void CalculateBestPathDistances(IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.DistancesInOrder[i] =
                    distanceMatrix.GetInstance().Distances[BestPath.Order[i] + NoOfCities * BestPath.Order[i + 1]];
            }
        }

        protected void CalculateDistance()
        {
            BestPath.Distance = 0;
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Distance += BestPath.DistancesInOrder[i];
            }
        }

        protected void CalculateCost()
        {
            BestPath.Cost = 0;

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                if (BestPath.DistancesInOrder[i] != FreeMatrix.Distances[
                    BestPath.Order[i] + NoOfCities * BestPath.Order[i + 1]])
                {
                    BestPath.Cost += CostMatrix.Instance.Value[
                        BestPath.Order[i] + NoOfCities * BestPath.Order[i + 1]];
                }
            }
        }

        protected void CalculateGoal(IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Goal[i] =
                    distanceMatrix.GetInstance().Goals[
                        BestPath.Order[i] * NoOfCities + BestPath.Order[i + 1]];
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


        protected static bool IsFreeRoad(int i, int indexOrigin, int indexDestination)
        {
            return BestPath.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination];
        }
    }
}
