using System;
using System.Collections.Generic;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP
{

    class AntColonyClassic : AntColonyAbstract
    {
        public override void SolveTSP()
        {

            InitializeParameters(DistanceMatrixForFreeRoads.Instance);
            FillAttractivenessMatrix(DistanceMatrixForFreeRoads.Instance);
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                List<int[]> pathList = new List<int[]>();
                double minimumPathInThisIteration = double.MaxValue;
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
                    UpdateTrialsMatrix(pathList[i], DistanceMatrixForFreeRoads.Instance);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    (minimumPathNumber, minimumPathInThisIteration) =
                        FindMinimumPathInThisIteration(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForFreeRoads.Instance);
                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber, DistanceMatrixForFreeRoads.Instance);

                    BestPath.Instance.Cost = 0;
                    CalculateDistance();

                    CalculateGoal(DistanceMatrixForFreeRoads.Instance);
                }
            }
        } //end of Ant Colony
    }
}
