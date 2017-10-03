using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

/**********************************************************
 * Process input from list of cities with coordinates     *
 * to matrix of distanses                                 *
 * *******************************************************/

namespace TSPTimeCost {
    class ProcessInputData {

        public List<Road> ReadInputFile() {
            List<Road> result = new List<Road>();

            using (var mappedFile1 = MemoryMappedFile.CreateFromFile("C:/Users/Adrian/Desktop/input.txt")) {
                using (Stream mmStream = mappedFile1.CreateViewStream()) {
                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII)) {
                        while (!sr.EndOfStream) {

                            Road toAdd = new Road();
                            var line = sr.ReadLine();
                            var lineWords = line.Split(' ');

                            toAdd.Beginning = lineWords[0];
                            toAdd.Ending = lineWords[1];
                            toAdd.Time = Convert.ToDouble(lineWords[2]);
                            toAdd.Cost = Convert.ToDouble(lineWords[3]);

                            result.Add(toAdd);
                        }
                    }
                }
            }

            return result;
        }

        public double ConvertDegreeAngleToDouble(double coordinates) {
            //Decimal degrees = 
            //   whole number of degrees, 
            //   plus minutes divided by 60, 
            //   plus seconds divided by 3600

            int degrees = (int)coordinates;
            double minutes = (coordinates - degrees) * 100;

            return degrees + (minutes / 60);
        }

        public Road ConvertTimeToDecimal(Road road) {
            road.TimeDecimal = ConvertDegreeAngleToDouble(road.Time);

            return road;
        }

/*        public void CalculateDistanceMatrix(List<Road> cities) {

            DistanceMatrix.Instance.value = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++) {
                for (int j = 0; j < cities.Count; j++) {
                    if (i == j) {
                        DistanceMatrix.Instance.value[j + i * cities.Count] = Double.MaxValue;
                    }
                    else {
                        DistanceMatrix.Instance.value[j + i * cities.Count] =
                            new Coordinates().distance((double)cities[i].Y, (double)cities[i].X, (double)cities[j].Y, (double)cities[j].X, 'K');
                    }
                }
            }
        }*/

        public void InitializeSingletons(int noOfCities)
        {
            BestPath.Instance.order = new int[noOfCities];
            DistanceMatrix.Instance.value = new double[noOfCities * noOfCities];

            //Fist bestPath is just cities in input order
            for (int i = 0; i < noOfCities; i++) {
                BestPath.Instance.order[i] = i;
            }
        }
    }
}
