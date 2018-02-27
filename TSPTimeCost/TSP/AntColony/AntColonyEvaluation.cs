using System.Collections.Generic;

namespace TSPTimeCost.TSP.AntColony
{

    class AntColonyEvaluation : AntColonyAbstract
    {
        //     private const double TimeCostEvaluation = 20.00;
        public override void SolveTSP()
        {

            InitializeParameters(EvaluatedMatrix);
            FillAttractivenessMatrix(EvaluatedMatrix);
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
                    UpdateTrialsMatrix(pathList[i], EvaluatedMatrix);
                }

                EvaporateTrialsMatrix();

                //if its last iteration
                if (j == NoOfIterations - 1)
                {
                    (minimumPathNumber, minimumPathInThisIteration) = FindMinimumPathInThisIteration(pathList,
                        minimumPathInThisIteration, minimumPathNumber, EvaluatedMatrix);

                    ReplaceBestPathWithCurrentBest(pathList, minimumPathInThisIteration, minimumPathNumber, EvaluatedMatrix);

                    CalculateGoal(EvaluatedMatrix);
                    CalculateCost();

                    CalculateDistance();
                }
            }
        }

        //end of Ant Colony
    }

}
