using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TESWebUI.Models;

namespace TESWebUI.TSPEngine
{
    public class ProcessInputData
    {
        private readonly HttpClient _httpClient;

        public ProcessInputData()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }

        public List<string> ReadCities(string incomingCities)
        {
            string[] cities = incomingCities.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            return cities.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public List<City> GetCitiesFromGoogleApi(List<string> cityNames)
        {
            List<City> cities = new List<City>();
            foreach (var cityName in cityNames)
            {
                City toAdd = new City { Name = cityName };

                JObject locationJson = LocationCallToGoogleMapsAPI(cityName);
                toAdd.Latitude = locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                cities.Add(toAdd);
            }

            return cities;
        }


        private JObject LocationCallToGoogleMapsAPI(string cityName)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

            Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
            getAsync.Wait();

            using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                   throw new ArgumentNullException(
                                       $"Execption on [{System.Reflection.MethodBase.GetCurrentMethod().Name}]"))
            {
                using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                {
                    JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                    return json;
                }
            }
        }


        public double CostBetweenTwoCitiesCall(City origin, City destination)
        {
            string url =
                $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

            Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
            getAsync.Wait();

            string content;
            using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                   throw new ArgumentNullException(
                                       $"Execption on [{System.Reflection.MethodBase.GetCurrentMethod().Name}]"))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
            double tollCost = Convert.ToDouble(node.InnerText);

            XmlNode vinietaNode = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
            double vinietaCost = Convert.ToDouble(vinietaNode.InnerText);

            double result = tollCost + vinietaCost;

            return result / 100;
        }

        public int DurationBetweenTwoCitiesByTollRoadCall(City origin, City destination)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

            Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
            getAsync.Wait();

            using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                   throw new ArgumentNullException(
                                       $"Execption on [{System.Reflection.MethodBase.GetCurrentMethod().Name}]"))
            {
                using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                {
                    JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                    try
                    {
                        return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                }
            }
        }

        public int GetDurationBetweenTwoCitiesByFreeRoadCall(City origin, City destination)
        {
            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";


            Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
            getAsync.Wait();

            using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                   throw new ArgumentNullException(
                                       $"Execption on [{System.Reflection.MethodBase.GetCurrentMethod().Name}]"))
            {
                using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                {
                    JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                    try
                    {
                        return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                }
            }

        }
    }
}
