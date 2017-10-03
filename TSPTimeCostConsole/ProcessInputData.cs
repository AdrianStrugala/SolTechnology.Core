using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using TSPTimeCost;

namespace TSPTimeCostConsole
{
    class ProcessInputData
    {

        public List<Road> ReadInputFile()
        {
            List<Road> result = new List<Road>();

            using (var mappedFile1 = MemoryMappedFile.CreateFromFile("C:/Users/Adrian/Desktop/input.txt"))
            {
                using (Stream mmStream = mappedFile1.CreateViewStream())
                {
                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII))
                    {
                        while (!sr.EndOfStream)
                        {

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

        public double ConvertDegreeAngleToDouble(double coordinates)
        {
            //Decimal degrees = 
            //   whole number of degrees, 
            //   plus minutes divided by 60, 
            //   plus seconds divided by 3600

            int degrees = (int)coordinates;
            double minutes = (coordinates - degrees) * 100;

            return degrees + (minutes / 60);
        }

        public Road ConvertTimeToDecimal(Road road)
        {
            road.TimeDecimal = ConvertDegreeAngleToDouble(road.Time);

            return road;
        }

        /*        public void CalculateDistanceMatrix(List<Road> cities) {

                    CostMatrix.Instance.value = new double[cities.Count * cities.Count];

                    for (int i = 0; i < cities.Count; i++) {
                        for (int j = 0; j < cities.Count; j++) {
                            if (i == j) {
                                CostMatrix.Instance.value[j + i * cities.Count] = Double.MaxValue;
                            }
                            else {
                                CostMatrix.Instance.value[j + i * cities.Count] =
                                    new Coordinates().distance((double)cities[i].Y, (double)cities[i].X, (double)cities[j].Y, (double)cities[j].X, 'K');
                            }
                        }
                    }
                }*/

        public void CreateMatrixes(List<Road> roads)
        {
            foreach (var road in roads)
            {
                if (road.Cost == 0)
                    AddToNoCostMatrix(road);
                else 
                    AddToCostMatrix(road);
            }

            foreach (var record in NoCostMatrix.Instance.value)
            {
                if(record == 0)
                    record = Double.MaxValue;
            }
        }
        //TODO
        private void AddToCostMatrix(Road road)
        {
            int i = ListOfCities.Instance.value.IndexOf(road.Beginning);
            int j = ListOfCities.Instance.value.IndexOf(road.Ending);

            CostMatrix.Instance.value[i * ListOfCities.Instance.value.Count + j] = road.Time;
            CostMatrix.Instance.value[j * ListOfCities.Instance.value.Count + i] = road.Time;
        }

        private void AddToNoCostMatrix(Road road)
        {
            int i = ListOfCities.Instance.value.IndexOf(road.Beginning);
            int j = ListOfCities.Instance.value.IndexOf(road.Ending);

            NoCostMatrix.Instance.value[i * ListOfCities.Instance.value.Count + j] = road.Time;
            NoCostMatrix.Instance.value[j * ListOfCities.Instance.value.Count + i] = road.Time;
        }

        public void CreateListOfCities(List<Road> roads)
        {
            foreach (var road in roads)
            {
                if (!ListOfCities.Instance.value.Contains(road.Beginning))
                {
                    ListOfCities.Instance.value.Add(road.Beginning);
                }
                if (!ListOfCities.Instance.value.Contains(road.Ending))
                {
                    ListOfCities.Instance.value.Add(road.Ending);
                }
            }
        }
        public void InitializeSingletons(int noOfCities)
        {
            CostMatrix.Instance.value = new double[noOfCities * noOfCities];
            NoCostMatrix.Instance.value = new double[noOfCities * noOfCities];
        }
    }
}

