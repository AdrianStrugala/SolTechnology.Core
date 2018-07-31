using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TravelingSalesmanProblem;

namespace TravelingSalesmanProblemTests.Benchmark
{
    class Benchmark
    {
        public static void RunTest(int numberOfExecutions, double[] distanceMatrix, int noOfCities, ITSP tspEngine, string nameOfEngine)
        {
            List<int[]> tspResults = new List<int[]>();
            List<double> resuts = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < numberOfExecutions; i++)
            {
                tspResults.Add(tspEngine.SolveTSP(distanceMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();


            //Prepair result
            foreach (var tspResult in tspResults)
            {
                double totalDistance = 0;

                for (int i = 0; i < noOfCities - 1; i++)
                {
                    totalDistance += distanceMatrix[tspResult[i] + tspResult[i + 1] * noOfCities];
                }
                resuts.Add(totalDistance);
            }


            //RESULTS
            var minimalDistance = resuts.Min(resut => resut);
            int recurrencePercentage = (resuts.Count(result => result.Equals(minimalDistance)) * 100) / numberOfExecutions;
            var averageTime = totalTime / numberOfExecutions;
            var averageDistance = resuts.Sum() / numberOfExecutions;

            System.IO.File.WriteAllText($@"..\..\..\..\..\docs\{nameOfEngine}_Benchmark_{noOfCities}Cities.txt",
                $"{nameOfEngine}: {noOfCities} Cities \n" +
                $"Minimal Distance: {minimalDistance} \n" +
                $"Average Distance: {averageDistance} \n" +
                $"Average Time: {averageTime} s \n" +
                $"Recurrence: {recurrencePercentage} %");
        }

    }
}
