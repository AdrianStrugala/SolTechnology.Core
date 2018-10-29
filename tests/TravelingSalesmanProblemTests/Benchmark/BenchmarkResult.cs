namespace TravelingSalesmanProblemTests.Benchmark
{
    public class BenchmarkResult
    {
        public string Algorithm { get; set; }

        public int NoOfCities { get; set; }

        public double AverageExecutionTime { get; set; }

        public double MinimalDistance { get; set; }

        public double AverageDistance { get; set; }

        public int? Recurrence { get; set; }
    }
}