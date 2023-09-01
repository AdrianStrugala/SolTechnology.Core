using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DreamTravel.TravelingSalesmanProblem
{
    public class ChatGPTNearestNeighbour : ITSP
    {
        public List<int> SolveTSP(List<double> distances)
        {
            int n = (int)Math.Sqrt(distances.Count);
            if (n < 3)
            {
                throw new ArgumentException("There should be at least 3 cities.");
            }

            var unvisitedCities = new HashSet<int>(Enumerable.Range(1, n - 2)); // Exclude first and last cities
            var tour = new List<int>();

            // Start from the first city (city 0)
            int currentCity = 0;
            tour.Add(currentCity);

            while (unvisitedCities.Count > 0)
            {
                int nearestCity = FindNearestNeighbor(currentCity, unvisitedCities, n, distances);
                tour.Add(nearestCity);
                currentCity = nearestCity;
                unvisitedCities.Remove(currentCity);
            }

            // Add the last city (city n-1) to complete the tour
            tour.Add(n - 1);

            return tour;
        }

        private int FindNearestNeighbor(int currentCity, HashSet<int> unvisitedCities, int n, List<double> distances)
        {
            int nearestCity = -1;
            double minDistance = double.MaxValue;

            foreach (int city in unvisitedCities)
            {
                double distance = distances[currentCity * n + city];
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestCity = city;
                }
            }

            return nearestCity;
        }

    }
}
