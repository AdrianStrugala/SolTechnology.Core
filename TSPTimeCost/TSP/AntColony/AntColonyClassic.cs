using System.Collections.Generic;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP.AntColony
{

    class AntColonyClassic : AntColonyAbstract
    {
        public override void SolveTSP()
        {
            InitializeParameters(FreeMatrix);
            FillAttractivenessMatrix(FreeMatrix);
            FillTrialsMatrix();

            //each iteration is one trip of the ants
            for (int j = 0; j < NoOfIterations; j++)
            {
                List<int[]> pathList = new List<int[]>();

                pathList = InitializePathList(pathList);

                //proceed for each ant
                for (int i = 0; i < NoOfAnts; i++)
                {
                    pathList[i] = CalculatePathForSingleAnt();
                }

                //must be separate, to not affect ants in the same iteration
                for (int i = 0; i < NoOfAnts; i++)
                {
                    UpdateTrialsMatrix(pathList[i], FreeMatrix);
                }

                EvaporateTrialsMatrix();
                
                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    int[] minimumPath = pathList[FindMinimumPathInListOfPaths(pathList, FreeMatrix)];
                    UpdateBestPath(minimumPath, FreeMatrix);
                }
            }
        } //end of Ant Colony
    }
}
