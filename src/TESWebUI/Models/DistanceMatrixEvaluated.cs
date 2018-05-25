using System.Collections.Generic;
using System.Threading.Tasks;
using TESWebUI.TSPEngine;

namespace TESWebUI.Models
{
    public sealed class DistanceMatrixEvaluated
    {
        public double[] Distances { get; set; }
        public double[] Goals { get; set; }
        public double[] Costs { get; set; }


        public DistanceMatrixEvaluated(int noOfCities)
        {

            Parallel.Invoke(
                () => Distances = new double[noOfCities * noOfCities],
                () => Goals = new double[noOfCities * noOfCities],
                () => Costs = new double[noOfCities * noOfCities]
            );


            Parallel.For(0, noOfCities * noOfCities, i =>
            {
                Distances[i] = -1;
                Goals[i] = -1;
                Costs[i] = -1;
            });
        }

    }
}