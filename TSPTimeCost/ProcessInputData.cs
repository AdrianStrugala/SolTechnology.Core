using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel_Ants;

/**********************************************************
 * Process input from list of cities                      *
 * to matrixes of distanses and cost                      *
 * *******************************************************/

namespace TSPTimeCost {
    class ProcessInputData {


        //DECIDE HOW TO TAKE INPUT
//        public List<Road> ReadInputFile() {
//            List<Road> result = new List<Road>();
//
//            using (var mappedFile1 = MemoryMappedFile.CreateFromFile("C:/Users/Adrian/Desktop/input.txt")) {
//                using (Stream mmStream = mappedFile1.CreateViewStream()) {
//                    using (StreamReader sr = new StreamReader(mmStream, Encoding.ASCII)) {
//                        while (!sr.EndOfStream) {
//
//                            Road toAdd = new Road();
//                            var line = sr.ReadLine();
//                            var lineWords = line.Split(' ');
//
//                            toAdd.Beginning = lineWords[0];
//                            toAdd.Ending = lineWords[1];
//                            toAdd.Time = Convert.ToDouble(lineWords[2]);
//                            toAdd.Cost = Convert.ToDouble(lineWords[3]);
//
//                            result.Add(toAdd);
//                        }
//                    }
//                }
//            }
//
//            return result;
//        }

        public List<string> ReadCities() {
            List<string> _cities = new List<string> { "Pisa","Verona", "Modena", "Bergamo" };

            return _cities;
        }


        public void CalculateDistanceMatrix(List<City> cities) {

            DistanceMatrix.Instance.value = new double[cities.Count * cities.Count];

            for (int i = 0; i < cities.Count; i++) {
                for (int j = 0; j < cities.Count; j++) {
                    if (i == j) {
                        DistanceMatrix.Instance.value[j + i * cities.Count] = Double.MaxValue;
                    }
                    else {
                        DistanceMatrix.Instance.value[j + i * cities.Count] =
                            GetDurationBetweenTwoCitiesByRoad(cities[i].Name, cities[j].Name);
                    }
                }
            }
        }


        private int GetDurationBetweenTwoCitiesByRoad(string origin, string destination) {

            string _url =
                string.Format(
                    "https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={0}&destinations={1}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k", origin, destination);

            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(_url);

            _request.Method = "GET";
            _request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            _request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse _response = (HttpWebResponse)_request.GetResponse();

            string _content;
            using (Stream _stream = _response.GetResponseStream()) {
                using (StreamReader _sr = new StreamReader(_stream)) {
                    _content = _sr.ReadToEnd();
                }
            }

            var _json = JObject.Parse(_content);

            return _json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();

        }

        public void InitializeSingletons(int noOfCities) {
            BestPath.Instance.order = new int[noOfCities];
            DistanceMatrix.Instance.value = new double[noOfCities * noOfCities];

            //Fist bestPath is just cities in input order
            for (int i = 0; i < noOfCities; i++) {
                BestPath.Instance.order[i] = i;
            }
        }


        public List<City> GetCitiesFromGoogleApi() {
            List<City> _cities = new List<City>();
            List<string> _cityNames = ReadCities();

            foreach (var _cityName in _cityNames) {
                City _toAdd = new City { Name = _cityName };

                JObject _locationJson = GetLocationJson(_cityName);
                _toAdd.Latitude = _locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                _toAdd.Longitude = _locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                _cities.Add(_toAdd);
            }

            return _cities;
        }

        JObject GetLocationJson(string cityName) {
            string _url =
            string.Format(
                "https://maps.googleapis.com/maps/api/geocode/json?address={0}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM",
                cityName);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse _response = (HttpWebResponse)request.GetResponse())
            using (Stream _stream = _response.GetResponseStream())
            using (StreamReader reader = new StreamReader(_stream)) {
                var _serializer = new JsonSerializer();

                using (var _jsonTextReader = new JsonTextReader(reader)) {
                    JObject _json = (JObject)_serializer.Deserialize(_jsonTextReader);
                    return _json;
                }
            }

        }
    }
}
