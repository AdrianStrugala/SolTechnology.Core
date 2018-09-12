using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml;
using DreamTravel.ExternalConnection.Interfaces;
using DreamTravel.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamTravel.ExternalConnection
{
    public class CallAPI : ICallAPI
    {
        private readonly HttpClient _httpClient;

        public CallAPI()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }

        }

        public (double, double) DowloadCostBetweenTwoCities(City origin, City destination)
        {
            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

                HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                double tollCost;
                double vinietaCost;
                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        var content = sr.ReadToEnd();

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(content);

                        XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
                        tollCost = Convert.ToDouble(node.InnerText);

                        XmlNode vinietaNode = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
                        vinietaCost = Convert.ToDouble(vinietaNode.InnerText);
                    }
                }

                return (tollCost / 100, vinietaCost / 100);
            }
            catch (Exception)
            {
                throw new InvalidDataException(
                    $"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");
            }
        }

        public double[] DowloadDurationMatrixByTollRoad(List<City> listOfCities)
        {
            double[] result = new double[listOfCities.Count * listOfCities.Count];

            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            try
            {
                for (int i = 0; i < listOfCities.Count; i++)
                {
                    string url =
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                    HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                    using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                           throw new ArgumentNullException(
                                               $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                    {
                        using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                        {
                            JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                            for (int j = 0; j < listOfCities.Count; j++)
                            {
                                result[j + i * listOfCities.Count] = json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                throw new InvalidDataException(e.Message);
            }
        }

        public double[] DowloadDurationMatrixByFreeRoad(List<City> listOfCities)
        {
            double[] result = new double[listOfCities.Count * listOfCities.Count];

            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            try
            {
                for (int i = 0; i < listOfCities.Count; i++)
                {
                    string url =
                        $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={listOfCities[i].Latitude},{listOfCities[i].Longitude}&destinations={coordinates}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                    HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                    using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                           throw new ArgumentNullException(
                                               $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                    {
                        using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                        {
                            JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                            for (int j = 0; j < listOfCities.Count; j++)
                            {
                                result[j + i * listOfCities.Count] = json["rows"][0]["elements"][j]["duration"]["value"].Value<int>();
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                throw new InvalidDataException(e.Message);
            }
        }
    }
}
