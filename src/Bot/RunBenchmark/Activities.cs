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
            TwoCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\twoCities.txt"));
            FourCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\fourCities.txt"));
            EightCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\eightCities.txt"));
            TwelveCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\twelveCities.txt"));
            SixteenCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\sixteenCities.txt"));
            TwentyCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\RunBenchmark\Benchmark\TestData\twentyCities.txt"));
        }


        [FunctionName(AntColonyBenchmarkFunctionName)]
        public static async Task AntColony(
            [ActivityTrigger] object input)
        {
            var tspEngine = new TravelingSalesmanProblem.AntColony();

            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwoCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, FourCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, EightCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwelveCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, SixteenCitiesMatrix, tspEngine, "AntColony");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwentyCitiesMatrix, tspEngine, "AntColony");
        }


        [FunctionName(GodBenchmarkFunctionName)]
        public static async Task God(
            [ActivityTrigger] object input)
        {
            var tspEngine = new TravelingSalesmanProblem.God();

            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwoCitiesMatrix, tspEngine, "God");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, FourCitiesMatrix, tspEngine, "God");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, EightCitiesMatrix, tspEngine, "God");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwelveCitiesMatrix, tspEngine, "God");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, SixteenCitiesMatrix, tspEngine, "God");
            await Benchmark.Benchmark.RunTest(NumberOfExecutions, TwentyCitiesMatrix, tspEngine, "God");
        }
    }
}
