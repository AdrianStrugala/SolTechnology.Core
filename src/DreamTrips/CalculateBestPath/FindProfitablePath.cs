using System.Threading.Tasks;
using DreamTravel.DreamTrips.CalculateBestPath.Interfaces;
using DreamTravel.DreamTrips.CalculateBestPath.Models;

namespace DreamTravel.DreamTrips.CalculateBestPath
{
    public class FindProfitablePath : IFindProfitablePath
    {
        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        public EvaluationMatrix Execute(EvaluationMatrix evaluationMatrix, int noOfCities)
        {
            Parallel.For(0, noOfCities, i =>
            {
                Parallel.For(0, noOfCities, j =>
                {
                    if (i != j)
                    {
                        int iterator = j + i * noOfCities;

                        //if toll takes more time than regular -> pretend it does not exist
                        if (evaluationMatrix.TollDistances[iterator] > evaluationMatrix.FreeDistances[iterator])
                        {
                            evaluationMatrix.TollDistances[iterator] = evaluationMatrix.FreeDistances[iterator];
                            evaluationMatrix.Costs[iterator] = 0;
                        }

                        if (IsTollRoadProfitable(evaluationMatrix, iterator))
                        {
                            evaluationMatrix.OptimalDistances[iterator] = evaluationMatrix.TollDistances[iterator];
                            evaluationMatrix.OptimalCosts[iterator] = evaluationMatrix.Costs[iterator];
                        }
                        else
                        {
                            evaluationMatrix.OptimalDistances[iterator] = evaluationMatrix.FreeDistances[iterator];
                            evaluationMatrix.OptimalCosts[iterator] = 0;
                        }
                    }
                });
            });

            return evaluationMatrix;
        }


        private static bool IsTollRoadProfitable(EvaluationMatrix evaluationMatrix, int iterator)
        {
            //roads using vinieta are never profitable
            if (evaluationMatrix.VinietaCosts[iterator] != 0)
            {
                return false;
            }

            // C_G=s×combustion×fuel price [€] = v x t x combustion x fuel 
            double gasolineCostFree =
                evaluationMatrix.FreeDistances[iterator] /
                3600.0 * RoadVelocity * RoadCombustion * FuelPrice;

            double gasolineCostToll =
                evaluationMatrix.TollDistances[iterator] /
                3600.0 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;

            //toll goal = (cost of gasoline + cost of toll fee) * time of toll
            double cost = (gasolineCostToll + evaluationMatrix.Costs[iterator]);
            double time = (evaluationMatrix.TollDistances[iterator] / 3600.0);
            double importance = (evaluationMatrix.TollDistances[iterator] * 1.0 /
                                 evaluationMatrix.FreeDistances[iterator] * 1.0);
            var tollGoal = cost * time * importance;
            var freeGoal = gasolineCostFree * (evaluationMatrix.FreeDistances[iterator] / 3600.0);


            evaluationMatrix.Goals[iterator] = tollGoal;

            return freeGoal > tollGoal;
        }
    }
}
