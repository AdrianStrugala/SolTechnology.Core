using Newtonsoft.Json;
using Xunit;

namespace TravelingSalesmanProblemTests.Benchmark
{
    [Collection("Benchmark")]
    public class AntColonyBenchmark
    {
        private const int NumberOfExecutions = 100;
        private readonly Configuration _config;

        private readonly double[] _twoCitiesMatrix;
        private readonly double[] _fourCitiesMatrix;
        private readonly double[] _eightCitiesMatrix;
        private readonly double[] _sixteenCitiesMatrix;
        private readonly double[] _twelveCitiesMatrix;
        private readonly double[] _twentyCitiesMatrix;

        readonly TravelingSalesmanProblem.AntColony _tspEngine;


        public AntColonyBenchmark()
        {
            _config = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText("Benchmark\\configuration.json"));
            _twoCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twoCities.txt"));
            _fourCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\fourCities.txt"));
            _eightCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\eightCities.txt"));
            _twelveCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twelveCities.txt"));
            _sixteenCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\sixteenCities.txt"));
            _twentyCitiesMatrix = JsonConvert.DeserializeObject<double[]>(System.IO.File.ReadAllText(@".\Benchmark\TestData\twentyCities.txt"));

            _tspEngine = new TravelingSalesmanProblem.AntColony();
        }


        [Fact]
        void TwoCities()
        {
            if (!_config.AntColony.TwoCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twoCitiesMatrix, _tspEngine, "AntColony");
        }

        [Fact]
        void FourCities()
        {
            if (!_config.AntColony.FourCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _fourCitiesMatrix, _tspEngine, "AntColony");
        }

        [Fact]
        void EightCities()
        {
            if (!_config.AntColony.EightCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _eightCitiesMatrix, _tspEngine, "AntColony");
        }

        [Fact]
        void TwelveCities()
        {
            if (!_config.AntColony.TwelveCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twelveCitiesMatrix, _tspEngine, "AntColony");
        }

        [Fact]
        void SixteenCities()
        {
            if (!_config.AntColony.SixteenCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _sixteenCitiesMatrix, _tspEngine, "AntColony");
        }

        [Fact]
        void TwentyCities()
        {
            if (!_config.AntColony.TwentyCities) { return; }

            Benchmark.RunTest(NumberOfExecutions, _twentyCitiesMatrix, _tspEngine, "AntColony");
        }
    }
}
