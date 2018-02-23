using System;
using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{

    class AntColonyWithLimit : AntColonyAbstract
    {
        private new static readonly int NoOfCities = Cities.Instance.ListOfCities.Count;
        private readonly double _limit;

        public AntColonyWithLimit(double limit)
        {
            _limit = limit;
        }

        public override void SolveTSP()
        {

            InitializeParameters(DistanceMatrixForTollRoads.Instance);
            FillAttractivenessMatrix(DistanceMatrixForTollRoads.Instance);
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                List<int[]> pathList = new List<int[]>();
                double minimumPathInThisIteration = Double.MaxValue;
                int minimumPathNumber = -1;

                pathList = InitializePathList(pathList);

                //proceed for each ant
                for (int i = 0; i < NoOfAnts; i++)
                {
                    pathList[i] = CalculatePathForSingleAnt();
                }
                //must be separate, to not affect ants in the same iteration
                for (int i = 0; i < NoOfAnts; i++)
                {
                    UpdateTrialsMatrix(pathList[i], DistanceMatrixForTollRoads.Instance);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    (minimumPathNumber, minimumPathInThisIteration) = FindMinimumPathInThisIteration(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForTollRoads.Instance);
                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForTollRoads.Instance);


                    List<TimeDifferenceAndCost> worthList = CalculateGoal();
                    worthList.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

                    double overallCost = 0;

                    overallCost = UpdateCostsAndBestPath(overallCost, worthList);

                    BestPath.Instance.Cost = overallCost;

                    CalculateDistance();
                }
            }
        }

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
                    BestPath.Instance.DistancesInOrder[item.Index] =
                        DistanceMatrixForFreeRoads.Instance.Distances[
                            BestPath.Instance.Order[item.Index] + NoOfCities * BestPath.Instance.Order[item.Index + 1]];

                    item.Goal = DistanceMatrixForFreeRoads.Instance.Goals[BestPath.Instance.Order[item.Index] + NoOfCities * BestPath.Instance.Order[item.Index + 1]];
                    BestPath.Instance.Goal[item.Index] = item.Goal;

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
                    TimeDifference =
                        DistanceMatrixForFreeRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination] -
                        DistanceMatrixForTollRoads.Instance.Distances[indexOrigin + NoOfCities * indexDestination],
                };
            return goalItem;
        }


        private List<TimeDifferenceAndCost> CalculateGoal()
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

                goalItem.Goal = IsFreeRoad(i, indexOrigin, indexDestination) ? DistanceMatrixForFreeRoads.Instance.Goals[indexOrigin + NoOfCities * indexDestination] : DistanceMatrixForTollRoads.Instance.Goals[indexOrigin + NoOfCities * indexDestination];

                BestPath.Instance.Goal[i] = goalItem.Goal;

                goalList.Add(goalItem);
            }

            return goalList;
        }
    }

}
