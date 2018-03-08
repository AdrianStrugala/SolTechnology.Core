using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP.AntColony
{

    class AntColonyWithLimit : AntColonyAbstract
    {

        private readonly double _limit;

        public AntColonyWithLimit(double limit)
        {
            _limit = limit;
        }

        public override void SolveTSP()
        {
            //stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            InitializeParameters(TollMatrix);
            FillAttractivenessMatrix(TollMatrix);
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                List<int[]> pathList = new List<int[]>();

                pathList = InitializePathList(pathList);

                //proceed for each ant
                Parallel.For(0, NoOfAnts, i =>
                    pathList[i] = CalculatePathForSingleAnt()
                );

                //must be separate, to not affect ants in the same iteration
                Parallel.For(0, NoOfAnts, i =>
                    UpdateTrialsMatrix(pathList[i], TollMatrix)
                );

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    int[] minimumPath = FindMinimumPathInListOfPaths(pathList, TollMatrix);
                    UpdateBestPath(minimumPath, TollMatrix);

                    List<TimeDifferenceAndCost> worthList = CalculateGoal();
                    worthList.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

                    double overallCost = 0;

                    overallCost = UpdateCostsAndBestPath(overallCost, worthList);

                    BestPath.Cost = overallCost;

                    CalculateDistance();
                }
            }
            //stopwatch
            BestPath.Instance.TimeOfExecution = stopwatch.Elapsed.ToString();
            stopwatch.Stop();
        }//end of ant colony

        private double UpdateCostsAndBestPath(double overallCost, List<TimeDifferenceAndCost> worthList)
        {
            foreach (var item in worthList)
            {
                if (item.TimeDifference == 0) continue;
                if (overallCost + item.FeeCost <= _limit)
                {
                    overallCost += item.FeeCost;
                }
                else
                {
                    BestPath.DistancesInOrder[item.Index] = FreeMatrix.Distances[BestPath.Order[item.Index] + NoOfCities * BestPath.Order[item.Index + 1]];

                    item.Goal = FreeMatrix.Goals[BestPath.Order[item.Index] + NoOfCities * BestPath.Order[item.Index + 1]];
                    BestPath.Goal[item.Index] = item.Goal;

                    item.TimeDifference = 0;
                }
            }
            return overallCost;
        }
        //end of Ant Colony


        private static TimeDifferenceAndCost InitializeGoalItem(int i, int indexOrigin, int indexDestination)
        {
            TimeDifferenceAndCost goalItem =
                new TimeDifferenceAndCost
                {
                    FeeCost = CostMatrix.Instance.Value[indexOrigin + NoOfCities * indexDestination],
                    Index = i,
                    TimeDifference = FreeMatrix.Distances[indexOrigin + NoOfCities * indexDestination] - TollMatrix.Distances[indexOrigin + NoOfCities * indexDestination],
                };
            return goalItem;
        }

        private List<TimeDifferenceAndCost> CalculateGoal()
        {
            List<City> cities = Cities.Instance.ListOfCities;
            List<TimeDifferenceAndCost> goalList = new List<TimeDifferenceAndCost>();

            for (int i = 0; i < NoOfCities - 1; i++)
            {
                City origin = cities[BestPath.Order[i]];
                City destination = cities[BestPath.Order[i + 1]];
                var indexOrigin = cities.IndexOf(origin);
                var indexDestination = cities.IndexOf(destination);

                TimeDifferenceAndCost goalItem = InitializeGoalItem(i, indexOrigin, indexDestination);

                goalItem.Goal = IsFreeRoad(i, indexOrigin, indexDestination) ? FreeMatrix.Goals[indexOrigin + NoOfCities * indexDestination] : TollMatrix.Goals[indexOrigin + NoOfCities * indexDestination];

                BestPath.Goal[i] = goalItem.Goal;

                goalList.Add(goalItem);
            }

            return goalList;
        }
    }

}
