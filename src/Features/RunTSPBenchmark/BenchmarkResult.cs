namespace DreamTravel.Features.RunTSPBenchmark
{
    public class BenchmarkResult
    {
        public string Algorithm { get; set; }

        public int NoOfCities { get; set; }

        public int AverageExecutionTime { get; set; }

        public int MinimalDistance { get; set; }

        public int AverageDistance { get; set; }

        public int? Recurrence { get; set; }
    }
}