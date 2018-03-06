using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TSPTimeCost.Singletons;

namespace TSPTimeCost.TSP.AntColony
{
    class AntColonyToll : AntColonyAbstract
    {
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
                    pathList[i] = CalculatePathForSingleAnt());

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
                }
            }
            //stopwatch
            BestPath.Instance.TimeOfExecution = stopwatch.Elapsed.ToString();
            stopwatch.Stop();
        }//end of Ant Colony
    }
}
