/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

using System.Collections.Generic;
using System.Threading.Tasks;

namespace TESWebUI.Models
{
    public sealed class DistanceMatrixEvaluated
    {
        private static double FuelPrice { get; } = 1.26;
        private static double RoadVelocity { get; } = 70;
        private static double HighwayVelocity { get; } = 120;
        private static double RoadCombustion { get; } = 0.06; //per km

        public DistanceMatrixEvaluated(int noOfCities)
        {

            Parallel.Invoke(
                () => Distances = new double[noOfCities * noOfCities],
                () => Goals = new double[noOfCities * noOfCities],
                () => Costs = new double[noOfCities * noOfCities]
            );


            for (int i = 0; i < noOfCities * noOfCities; i++)
            {
                Distances[i] = -1;
                Goals[i] = -1;
                Costs[i] = -1;
            }
        }

        internal void DownloadData(List<City> listOfCities)
        {
            for (int i = 0; i < listOfCities.Count; i++)
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {

                    if (i == j)
                    {
                        Distances[j + i * listOfCities.Count] = double.MaxValue;
                        Goals[j + i * listOfCities.Count] = double.MaxValue;
                        Costs[j + i * listOfCities.Count] = double.MaxValue;
                    }
                    else
                    {
                        int timeFree = -1;
                        int timeToll = -1;
                        double costToll = -1;

                        Parallel.Invoke(
                            () => timeFree =
                                ProcessInputData.GetDurationBetweenTwoCitiesByFreeRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => timeToll =
                                ProcessInputData.GetDurationBetweenTwoCitiesByTollRoad(listOfCities[i],
                                    listOfCities[j]),
                            () => costToll =
                                ProcessInputData.GetCostBetweenTwoCities(listOfCities[i], listOfCities[j])
                        );
                        // C_G=s×combustion×fuel price [€] = v x t x combustion x fuel 
                        double gasolineCostFree =
                            timeFree /
                            3600.0 * RoadVelocity * RoadCombustion * FuelPrice;

                        // 
                        double gasolineCostToll =
                            timeToll /
                            3600.0 * HighwayVelocity * RoadCombustion * 1.25 * FuelPrice;


                        //toll goal = (cost of gasoline + cost of toll fee) * time of toll
                        double cost = (gasolineCostToll + costToll);
                        double time = (timeToll / 3600.0);
                        double importance = (timeToll * 1.0 / timeFree * 1.0);
                        double tollGoal = cost * time * importance;

                        var freeGoal =
                            gasolineCostFree * (timeFree / 3600.0);

                        if (freeGoal < tollGoal)
                        {
                            Distances[j + i * listOfCities.Count] = timeFree;
                            Goals[j + i * listOfCities.Count] = freeGoal;
                            Costs[j + i * listOfCities.Count] = 0;
                        }
                        else
                        {
                            Distances[j + i * listOfCities.Count] = timeToll;
                            Goals[j + i * listOfCities.Count] = tollGoal;
                            Costs[j + i * listOfCities.Count] = costToll;
                        }
                    }
                }
            }
        }


        public double[] Distances { get; set; }
        public double[] Goals { get; set; }
        public double[] Costs { get; set; }

    }
}