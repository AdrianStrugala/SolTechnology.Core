using Newtonsoft.Json;
using Xunit;

namespace TravelingSalesmanProblemTests.Benchmark
{
    using System.Threading.Tasks;

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
        async Task TwoCities()
        {
            if (!_config.God.TwoCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _twoCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        async Task FourCities()
        {
            if (!_config.God.FourCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _fourCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        async Task EightCities()
        {
            if (!_config.God.EightCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _eightCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        async Task TwelveCities()
        {
            if (!_config.God.TwelveCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _twelveCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        async Task SixteenCities()
        {
            if (!_config.God.SixteenCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _sixteenCitiesMatrix, _tspEngine, "God");
        }

        [Fact]
        async Task TwentyCities()
        {
            if (!_config.God.TwentyCities) { return; }

            await Benchmark.RunTest(NumberOfExecutions, _twentyCitiesMatrix, _tspEngine, "God");
        }
    }
}
