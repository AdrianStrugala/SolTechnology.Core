using System.Collections.Generic;

namespace TSPTimeCost.TSP.AntColony
{

    class AntColonyEvaluation : AntColonyAbstract
    {
        public override void SolveTSP()
        {
            InitializeParameters(EvaluatedMatrix);
            FillAttractivenessMatrix(EvaluatedMatrix);
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
                    UpdateTrialsMatrix(pathList[i], EvaluatedMatrix);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    int[] minimumPath = pathList[FindMinimumPathInListOfPaths(pathList, EvaluatedMatrix)];
                    UpdateBestPath(minimumPath, EvaluatedMatrix);
                }
            }
        }
        //end of Ant Colony
    }

}
