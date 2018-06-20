using System.Threading.Tasks;
using TravelingSalesmanProblem.Models;

namespace DreamTravel.Models
{
    public sealed class EvaluationMatrix : IEvaluationMatrix
    {
        public double[] FreeDistances { get; set; }
        public double[] TollDistances { get; set; }
        public double[] OptimalDistances { get; set; }
        public double[] Goals { get; set; }
        public double[] Costs { get; set; }
        public double[] OptimalCosts { get; set; }

        public EvaluationMatrix(int noOfCities)
        {
            Parallel.Invoke(
                () => FreeDistances = new double[noOfCities * noOfCities],
                () => TollDistances = new double[noOfCities * noOfCities],
                () => OptimalDistances = new double[noOfCities * noOfCities],
                () => Goals = new double[noOfCities * noOfCities],
                () => Costs = new double[noOfCities * noOfCities],
                () => OptimalCosts = new double[noOfCities * noOfCities]
            );

            Parallel.For(0, noOfCities * noOfCities, i =>
            {
                FreeDistances[i] = -1;
                TollDistances[i] = -1;
                OptimalDistances[i] = -1;
                Goals[i] = -1;
                Costs[i] = -1;
                OptimalCosts[i] = -1;
            });
        }
    }
}