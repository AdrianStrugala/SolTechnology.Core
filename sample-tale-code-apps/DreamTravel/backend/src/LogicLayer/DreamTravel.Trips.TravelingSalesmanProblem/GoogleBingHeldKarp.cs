using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DreamTravel.TravelingSalesmanProblem
{
    public class GoogleBingHeldKarp : ITSP
    {
        public List<int> SolveTSP(List<double> distances)
        {
            // Create a graph representation of the TSP problem.
            var graph = new Graph(distances.Count);
            for (int i = 0; i < distances.Count; i++)
            {
                for (int j = i + 1; j < distances.Count; j++)
                {
                    graph.AddEdge(i, j, distances[i * distances.Count + j]);
                }
            }

            // Solve the TSP problem using the Held-Karp algorithm.
            var solution = graph.HeldKarp(distances);

            // Return the list of city indexes in the solution.
            return solution.ToList();
        }
    }

    public class Graph
    {
        private readonly int _numVertices;
        private readonly List<List<int>> _adjacencyList;

        public Graph(int numVertices)
        {
            _numVertices = numVertices;
            _adjacencyList = new List<List<int>>(numVertices);
            for (int i = 0; i < numVertices; i++)
            {
                _adjacencyList.Add(new List<int>());
            }
        }

        public void AddEdge(int vertex1, int vertex2, double weight)
        {
            _adjacencyList[vertex1].Add(vertex2);
            _adjacencyList[vertex2].Add(vertex1);
        }

        public List<int> GetAdjacentVertices(int vertex)
        {
            return _adjacencyList[vertex];
        }

        public int GetNumVertices()
        {
            return _numVertices;
        }

        public List<int> HeldKarp(List<double> distances)
        {
            // Create a table to store the minimum cost of a tour that starts at a given city and visits a subset of the other cities.
            var table = new List<List<double>>(_numVertices);
            for (int i = 0; i < _numVertices; i++)
            {
                table.Add(new List<double>());
                for (int j = 0; j < _numVertices; j++)
                {
                    table[i].Add(double.PositiveInfinity);
                }
            }

            // Initialize the table.
            table[0][0] = 0;

            // Iterate over all possible tours.
            for (int numCities = 2; numCities <= _numVertices; numCities++)
            {
                for (int startVertex = 0; startVertex < _numVertices; startVertex++)
                {
                    for (int endVertex = 0; endVertex < _numVertices; endVertex++)
                    {
                        if (startVertex != endVertex)
                        {
                            for (int intermediateVertex = 0; intermediateVertex < _numVertices; intermediateVertex++)
                            {
                                if (intermediateVertex != startVertex && intermediateVertex != endVertex)
                                {
                                    // The cost of the tour that starts at `startVertex`, visits `intermediateVertex`, and then ends at `endVertex` is the minimum of the following:
                                    //   - The cost of the tour that starts at `startVertex` and visits all of the cities in `{intermediateVertex, endVertex}`.
                                    //   - The cost of the tour that starts at `intermediateVertex`, visits all of the cities in `{startVertex, endVertex}`.
                                    var cost = table[startVertex][intermediateVertex] + table[intermediateVertex][endVertex] + GetEdgeWeight(startVertex, endVertex, distances);
                                    table[startVertex][endVertex] = Math.Min(table[startVertex][endVertex], cost);
                                }
                            }
                        }
                    }
                }
            }

            // The optimal tour is the tour with the minimum cost in the table.
            var solution = new List<int>();
            double minCost = table[0][_numVertices - 1];
            for (int i = 0; i < _numVertices; i++)
            {
                if (table[0][i] < minCost)
                {
                    minCost = table[0][i];
                    solution = new List<int>();
                    solution.Add(i);
                }
                else if (table[0][i] == minCost)
                {
                    solution.Add(i);
                }
            }

            return solution;
        }

        private double GetEdgeWeight(int startVertex, int endVertex, List<double> distances)
        {
            // Get the weight of the edge between the two vertices.
            var weight = distances[startVertex * distances.Count + endVertex];

            // If the weight is not defined, return a large number.
            if (weight == -1)
            {
                return double.PositiveInfinity;
            }

            return weight;
        }
    }
}
