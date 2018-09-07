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
        private readonly double[] _twelveCitiesMatrix;
        private readonly double[] _twentyCitiesMatrix;

        readonly TravelingSalesmanProblem.God _tspEngine;

        public GodBenchmark()
        {
            _config = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText("Benchmark\\configuration.json"));
            _twoCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twoCities.txt"));
            _fourCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\fourCities.txt"));
            _eightCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\eightCities.txt"));
            _twelveCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twelveCities.txt"));
            _sixteenCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\sixteenCities.txt"));
            _twentyCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twentyCities.txt"));

            _tspEngine = new TravelingSalesmanProblem.God();
        }


        [Fact]
        void TwoCities()
        {
            if (!_config.God.TwoCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twoCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        void FourCities()
        {
            if (!_config.God.FourCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _fourCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        void EightCities()
        {
            if (!_config.God.EightCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _eightCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        void TwelveCities()
        {
            if (!_config.God.TwelveCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twelveCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        void SixteenCities()
        {
            if (!_config.God.SixteenCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _sixteenCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        void TwentyCities()
        {
            if (!_config.God.TwentyCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twentyCitiesMatrix, _tspEngine, "God");
        }
    }
}
