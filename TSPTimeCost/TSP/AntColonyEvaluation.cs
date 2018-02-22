using System;
using System.Collections.Generic;
using TSPTimeCost.Models;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{

    class AntColonyEvaluation : AntColony
    {
        private const double TimeCostEvaluation = 20.00;
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

                    NormalizeDistances();
                }
            }
        }

        private double UpdateCostsAndBestPath(double overallCost, List<TimeDifferenceAndCost> worthList)
        {
            foreach (var item in worthList)
            {
                if (item.Goal <= TimeCostEvaluation)
                {
                    overallCost += item.FeeCost;
                }

                else
                {
                    BestPath.Instance.DistancesInOrder[item.Index] =
                        DistanceMatrixForFreeRoads.Instance.Value[
                            BestPath.Instance.Order[item.Index] + NoOfCities * BestPath.Instance.Order[item.Index + 1]];

                    item.Goal = CalculateGoalForFreeRoad(Cities.Instance.ListOfCities, BestPath.Instance.Order[item.Index],
                        BestPath.Instance.Order[item.Index + 1],item).Goal;

                    BestPath.Instance.Goal[item.Index] = CalculateGoalForFreeRoad(Cities.Instance.ListOfCities, BestPath.Instance.Order[item.Index],
                        BestPath.Instance.Order[item.Index + 1], item).Goal;
                    item.TimeDifference = 0;
                }
            }
            return overallCost;
        }
        //end of Ant Colony



    }

}
