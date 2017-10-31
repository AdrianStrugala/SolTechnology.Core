using System;
using System.Collections.Generic;
using TSPTimeCost.Models;

namespace TSPTimeCost.TSP
{

    class AntColonyClassic : AntColony
    {

        public override void AntColonySingleThread(List<City> cities)
        {

            InitializeParameters(DistanceMatrixForFreeRoads.Instance);
            FillAttractivenessMatrix(DistanceMatrixForFreeRoads.Instance);
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
                    UpdateTrialsMatrix(pathList[i], DistanceMatrixForFreeRoads.Instance);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == noOfIterations - 1)
                {
                    (minimumPathNumber, minimumPathInThisIteration) =
                        FindMinimumPathInThisIteration(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForFreeRoads.Instance);
                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForFreeRoads.Instance);

                    BestPath.Instance.Cost = 0;
                    NormalizeDistances();
                }
            }
        } //end of Ant Colony
    }
}
