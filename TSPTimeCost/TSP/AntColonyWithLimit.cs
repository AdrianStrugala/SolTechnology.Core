using System;
using System.Collections.Generic;
using System.Linq;
using TSPTimeCost.Models;

namespace TSPTimeCost.TSP
{

    class AntColonyWithLimit : AntColony
    {
        private readonly double _limit;

        public AntColonyWithLimit(double limit)
        {
            _limit = limit;
        }

        public override void AntColonySingleThread(List<City> cities)
        {

            InitializeParameters(DistanceMatrixForTollRoads.Instance);
            FillAttractivenessMatrix(DistanceMatrixForTollRoads.Instance);
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < noOfIterations; j++)
            {
                List<int[]> pathList = new List<int[]>();
                double minimumPathInThisIteration = Double.MaxValue;
                int minimumPathNumber = -1;

                pathList = InitializePathList(pathList);

                //proceed for each ant
                for (int i = 0; i < noOfAnts; i++)
                {
                    pathList[i] = CalculatePathForSingleAnt();
                }
                //must be separate, to not affect ants in the same iteration
                for (int i = 0; i < noOfAnts; i++)
                {
                    UpdateTrialsMatrix(pathList[i], DistanceMatrixForTollRoads.Instance);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == noOfIterations - 1)
                {
                    (minimumPathNumber, minimumPathInThisIteration) = FindMinimumPathInThisIteration(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForTollRoads.Instance);
                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForTollRoads.Instance);


                    List<TimeDifferenceAndCost> worthList = CalculateGoal(cities);
                    worthList.Sort((x, y) => 1 * x.Goal.CompareTo(y.Goal));

                    double overallCost = 0;

                    overallCost = UpdateCostsAndBestPath(overallCost, worthList);

                    BestPath.Instance.Cost = overallCost;

                    NormalizeDistances();
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
                        DistanceMatrixForFreeRoads.Instance.Value[
                            BestPath.Instance.Order[item.Index] + noOfPoints * BestPath.Instance.Order[item.Index + 1]];

                    item.Goal = GoalFreeRoad;
                    BestPath.Instance.Goal[item.Index] = GoalFreeRoad;
                    item.TimeDifference = 0;
                }
            }
            return overallCost;
        }
        //end of Ant Colony



    }

}
