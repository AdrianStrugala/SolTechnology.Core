using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace TravelingSalesmanProblemTests.Benchmark
{
    [Collection("Benchmark")]
    public class GodBenchmark
    {
        private const int NumberOfExecutions = 50;
        private readonly Configuration _config;

        private readonly double[] _twoCitiesMatrix;
        private readonly double[] _fourCitiesMatrix;
        private readonly double[] _eightCitiesMatrix;
        private readonly double[] _sixteenCitiesMatrix;

        readonly TravelingSalesmanProblem.God _tspEngine;

        public GodBenchmark()
        {
            _config = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText("Benchmark\\configuration.json"));
            _twoCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twoCities.txt"));
            _fourCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\fourCities.txt"));
            _eightCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\eightCities.txt"));
            _sixteenCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\sixteenCities.txt"));

            _tspEngine = new TravelingSalesmanProblem.God();
        }


        [Fact]
        void TwoCities()
        {
            if (!_config.God.TwoCities) { return; }

            //Arrange
            int NoOfCities = 2;

            List<int[]> TSPResults = new List<int[]>();
            List<double> resuts = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < NumberOfExecutions; i++)
            {
                TSPResults.Add(_tspEngine.SolveTSP(_twoCitiesMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();


            //Prepair result
            foreach (var TSPResult in TSPResults)
            {
                double totalDistance = 0;

                for (int i = 0; i < NoOfCities - 1; i++)
                {
                    totalDistance += _twoCitiesMatrix[TSPResult[i] + TSPResult[i + 1] * NoOfCities];
                }
                resuts.Add(totalDistance);
            }


            //RESULTS
            var minimalDistance = resuts.Min(resut => resut);
            var recurrencePercentage = (resuts.Count(result => result.Equals(minimalDistance)) * 100) / NumberOfExecutions;
            var averageTime = totalTime / NumberOfExecutions;

            System.IO.File.WriteAllText(@"..\..\..\..\..\docs\God_Benchmark_TwoCities.txt",
                $"God: Two Cities \n" +
                $"Minimal Distance: {minimalDistance} \n" +
                $"Average Time: {averageTime} s \n" +
                $"Recurrence: {recurrencePercentage} %");
        }

        [Fact]
        void FourCities()
        {
            if (!_config.God.FourCities) { return; }

            //Arrange
            int NoOfCities = 4;

            List<int[]> TSPResults = new List<int[]>();
            List<double> resuts = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < NumberOfExecutions; i++)
            {
                TSPResults.Add(_tspEngine.SolveTSP(_fourCitiesMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();


            //Prepair result
            foreach (var TSPResult in TSPResults)
            {
                double totalDistance = 0;

                for (int i = 0; i < NoOfCities - 1; i++)
                {
                    totalDistance += _fourCitiesMatrix[TSPResult[i] + TSPResult[i + 1] * NoOfCities];
                }
                resuts.Add(totalDistance);
            }


            //RESULTS
            var minimalDistance = resuts.Min(resut => resut);
            var recurrencePercentage = (resuts.Count(result => result.Equals(minimalDistance)) * 100) / NumberOfExecutions;
            var averageTime = totalTime / NumberOfExecutions;

            System.IO.File.WriteAllText(@"..\..\..\..\..\docs\God_Benchmark_FourCities.txt",
                $"God: Four Cities \n" +
                $"Minimal Distance: {minimalDistance} \n" +
                $"Average Time: {averageTime} s \n" +
                $"Recurrence: {recurrencePercentage} %");
        }

        [Fact]
        void EightCities()
        {
            if (!_config.God.EightCities) { return; }

            //Arrange
            int NoOfCities = 8;

            List<int[]> TSPResults = new List<int[]>();
            List<double> resuts = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < NumberOfExecutions; i++)
            {
                TSPResults.Add(_tspEngine.SolveTSP(_eightCitiesMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();


            //Prepair result
            foreach (var TSPResult in TSPResults)
            {
                double totalDistance = 0;

                for (int i = 0; i < NoOfCities - 1; i++)
                {
                    totalDistance += _eightCitiesMatrix[TSPResult[i] + TSPResult[i + 1] * NoOfCities];
                }
                resuts.Add(totalDistance);
            }


            //RESULTS
            var minimalDistance = resuts.Min(resut => resut);
            var recurrencePercentage = (resuts.Count(result => result.Equals(minimalDistance)) * 100) / NumberOfExecutions;
            var averageTime = totalTime / NumberOfExecutions;

            System.IO.File.WriteAllText(@"..\..\..\..\..\docs\God_Benchmark_EightCities.txt",
                $"God: Eight Cities \n" +
                $"Minimal Distance: {minimalDistance} \n" +
                $"Average Time: {averageTime} s \n" +
                $"Recurrence: {recurrencePercentage} %");
        }

        [Fact]
        void SixteenCities()
        {
            if (!_config.God.SixteenCities) { return; }

            //Arrange
            int NoOfCities = 16;

            List<int[]> TSPResults = new List<int[]>();
            List<double> resuts = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < NumberOfExecutions; i++)
            {
                TSPResults.Add(_tspEngine.SolveTSP(_sixteenCitiesMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();


            //Prepair result
            foreach (var TSPResult in TSPResults)
            {
                double totalDistance = 0;

                for (int i = 0; i < NoOfCities - 1; i++)
                {
                    totalDistance += _sixteenCitiesMatrix[TSPResult[i] + TSPResult[i + 1] * NoOfCities];
                }
                resuts.Add(totalDistance);
            }


            //RESULTS
            var minimalDistance = resuts.Min(resut => resut);
            var recurrencePercentage = (resuts.Count(result => result.Equals(minimalDistance)) * 100) / NumberOfExecutions;
            var averageTime = totalTime / NumberOfExecutions;

            System.IO.File.WriteAllText(@"..\..\..\..\..\docs\God_Benchmark_SixteenCities.txt",
                $"God: Sixteen Cities \n" +
                $"Minimal Distance: {minimalDistance} \n" +
                $"Average Time: {averageTime} s \n" +
                $"Recurrence: {recurrencePercentage} %");
        }
    }
}
