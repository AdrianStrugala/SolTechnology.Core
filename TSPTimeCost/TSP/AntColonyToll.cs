using System;
using System.Collections.Generic;
using TSPTimeCost.Models;

namespace TSPTimeCost.TSP
{

    class AntColonyToll : AntColony
    {
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

                    var worthList = CalculateGoal(cities);

                    BestPath.Instance.Cost = 0;
                    foreach (var item in worthList)
                    {
                        if (item.TimeDifference == 0) continue;
                        BestPath.Instance.Cost += item.FeeCost;
                    }

                    NormalizeDistances();
                }
            }
        }//end of Ant Colony

    }

}
