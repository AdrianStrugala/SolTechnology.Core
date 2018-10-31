using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TravelingSalesmanProblem;

namespace TravelingSalesmanProblemTests.Benchmark
{
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    static class Benchmark
    {
        private static HttpClient _httpClient;

        private const string BenchmarkServiceUrl = "http://dreamtravel-benchmark.azurewebsites.net/PostBenchmarkResult";

        public static async Task RunTest(int numberOfExecutions, List<double> distanceMatrix, ITSP algorithm, string nameOfAlgorithm)
        {
            //Arrange
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

            BenchmarkResult benchmarkResult = new BenchmarkResult();

            int noOfCities = (int)Math.Sqrt(distanceMatrix.Count);

            List<List<int>> tspResults = new List<List<int>>();
            List<double> results = new List<double>();


            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < numberOfExecutions; i++)
            {
                tspResults.Add(algorithm.SolveTSP(distanceMatrix));
            }

            var totalTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Stop();



            //Prepare result
            foreach (var tspResult in tspResults)
            {
                double totalDistance = 0;
                for (int i = 0; i < noOfCities - 1; i++)
                {
                    totalDistance += distanceMatrix[tspResult[i] + tspResult[i + 1] * noOfCities];
                }
                results.Add(totalDistance);
            }


            //RESULTS
            benchmarkResult.Algorithm = nameOfAlgorithm;
            benchmarkResult.NoOfCities = noOfCities;
            benchmarkResult.MinimalDistance = (int)results.Min(result => result);
            benchmarkResult.Recurrence = (results.Count(result => result.Equals(benchmarkResult.MinimalDistance)) * 100) / numberOfExecutions;
            benchmarkResult.AverageExecutionTime = (int)(totalTime / numberOfExecutions * 1000);
            benchmarkResult.AverageDistance = (int)(results.Sum() / numberOfExecutions);

            //Request to DreamTravel.Benchmark
            var response = await _httpClient.PostAsJson(BenchmarkServiceUrl, benchmarkResult);

            System.IO.File.WriteAllText($@"..\..\..\..\..\docs\{nameOfAlgorithm}_Benchmark_{noOfCities}Cities.txt",
                $"{nameOfAlgorithm}: {noOfCities} Cities \n" +
                $"Minimal Distance: {benchmarkResult.MinimalDistance} \n" +
                $"Average Distance: {benchmarkResult.AverageDistance} \n" +
                $"Average Time: { benchmarkResult.AverageExecutionTime} ms \n" +
                $"Recurrence: {benchmarkResult.Recurrence} % \n" +
                $"Request response status: {response.StatusCode}");



        }

        public static async Task<HttpResponseMessage> PostAsJson(this HttpClient httpClient, string requestUri, object content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
            };

            return await httpClient.SendAsync(request);
        }
    }
}
