using System.Collections;
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

        protected static int NoOfCities = Cities.Instance.ListOfCities.Count;

        public abstract void SolveTSP();

        protected static void RewriteFoundPathToBestPath(List<int> path)
        {
            for (int i = 0; i < NoOfCities; i++)
            {
                BestPath.Order[i] = path[i];
            }
        }

        protected void UpdateBestPath(IEnumerable minimumPath, IDistanceMatrix distanceMatrix)
        {
            if (minimumPath is List<int>)
            {
                RewriteFoundPathToBestPath((List<int>)minimumPath);
            }

            else if (minimumPath is List<FanAndStar>)
            {
                RewriteFoundPathToBestPath((List<FanAndStar>)minimumPath);
            }

            else if (minimumPath is int[])
            {
                RewriteFoundPathToBestPath((int[])minimumPath);
            }

            CalculateBestPathDistances(distanceMatrix);
            CalculateDistance();
            CalculateCost();
            CalculateGoal(distanceMatrix);
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

            if (path != null)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    result += distanceMatrix.GetInstance().Distances[path[i] * path.Length + path[i + 1]];
                }
            }

            return result;
        }

        protected int FindMinimumPathInListOfPaths(List<int[]> pathList, IDistanceMatrix distanceMatrix)
        {
            double min = double.MaxValue;
            int nr = -1;
            double[] distances = new double[pathList.Count];

            for (int i = 0; i < pathList.Count; i++)
            {
                distances[i] = CalculateDistanceInPath(pathList[i], distanceMatrix);

                if (distances[i] < min)
                {
                    min = distances[i];
                    nr = i;
                }
            }
            return nr;
        }

        protected static bool IsFreeRoad(int i, int indexOrigin, int indexDestination)
        {
            return BestPath.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination];
        }
    }
}
