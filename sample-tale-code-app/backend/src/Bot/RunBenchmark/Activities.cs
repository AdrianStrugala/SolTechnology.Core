﻿using DreamTravel.DreamTrips.RunTSPBenchmark;
using DreamTravel.DreamTrips.RunTSPBenchmark.TestData;

namespace DreamTravel.Bot.RunBenchmark
{
    using Microsoft.Azure.WebJobs;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    public static class Activities
    {
        public const string AntColonyBenchmarkFunctionName = "AntColonyBenchmark";
        public const string GodBenchmarkFunctionName = "GodBenchmark";

        private const int NumberOfExecutions = 100;

        private static readonly double[] TwoCitiesMatrix;
        private static readonly double[] FourCitiesMatrix;
        private static readonly double[] EightCitiesMatrix;
        private static readonly double[] SixteenCitiesMatrix;
        private static readonly double[] TwelveCitiesMatrix;
        private static readonly double[] TwentyCitiesMatrix;

        static Activities()
        {
            TwoCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.twoCities);
            FourCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.fourCities);
            EightCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.eightCities);
            TwelveCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.twelveCities);
            SixteenCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.sixteenCities);
            TwentyCitiesMatrix = JsonConvert.DeserializeObject<double[]>(TestData.twentyCities);
        }


        [FunctionName(AntColonyBenchmarkFunctionName)]
        public static async Task AntColony(
            [ActivityTrigger] object input)
        {
            var tspEngine = new TravelingSalesmanProblem.AntColony();

            await Benchmark.RunTest(NumberOfExecutions, TwoCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.RunTest(NumberOfExecutions, FourCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.RunTest(NumberOfExecutions, EightCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.RunTest(NumberOfExecutions, TwelveCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.RunTest(NumberOfExecutions, SixteenCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.RunTest(NumberOfExecutions, TwentyCitiesMatrix, tspEngine, "AntColony");
        }


        [FunctionName(GodBenchmarkFunctionName)]
        public static async Task God(
            [ActivityTrigger] object input)
        {
            var tspEngine = new TravelingSalesmanProblem.God();

            await Benchmark.RunTest(NumberOfExecutions, TwoCitiesMatrix, tspEngine, "God");
            await Benchmark.RunTest(NumberOfExecutions, FourCitiesMatrix, tspEngine, "God");
            await Benchmark.RunTest(NumberOfExecutions, EightCitiesMatrix, tspEngine, "God");
            await Benchmark.RunTest(NumberOfExecutions, TwelveCitiesMatrix, tspEngine, "God");
            await Benchmark.RunTest(NumberOfExecutions, SixteenCitiesMatrix, tspEngine, "God");
            await Benchmark.RunTest(NumberOfExecutions, TwentyCitiesMatrix, tspEngine, "God");
        }
    }
}