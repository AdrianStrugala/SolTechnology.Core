using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{
    abstract class TSP
    {
        private static readonly int NoOfCities = Cities.Instance.ListOfCities.Count;

        public abstract void SolveTSP();

        protected void CalculateDistance()
        {
            BestPath.Instance.Distance = 0;
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Instance.Distance += BestPath.Instance.DistancesInOrder[i];
            }
        }

        protected void CalculateCost()
        {
            BestPath.Instance.Cost = 0;

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                if (BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForTollRoads.Instance.Distances[
                    BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]])
                {
                    BestPath.Instance.Cost += CostMatrix.Instance.Value[
                        BestPath.Instance.Order[i] + NoOfCities * BestPath.Instance.Order[i + 1]];
                }
            }
        }

        protected void CalculateGoal(IDistanceMatrix distanceMatrix)
        {
            for (int i = 0; i < NoOfCities - 1; i++)
            {
                BestPath.Instance.Goal[i] =
                    distanceMatrix.GetInstance().Goals[
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


        protected static bool IsFreeRoad(int i, int indexOrigin, int indexDestination)
        {
            return BestPath.Instance.DistancesInOrder[i] == DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination];
        }

      
    }
}
