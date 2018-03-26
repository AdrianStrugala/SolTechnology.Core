using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TESWebUI.Models;

namespace TESWebUI
{
    public class ProcessInputData
    {

        public static List<string> ReadCities(string incomingCities)
        {
            string[] cities = incomingCities.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            return cities.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public static List<City> GetCitiesFromGoogleApi(List<string> cityNames)
        {
            List<City> cities = new List<City>();
            foreach (var cityName in cityNames)
            {
                City toAdd = new City { Name = cityName };

                JObject locationJson = GetLocationJson(cityName);
                toAdd.Latitude = locationJson["results"][0]["geometry"]["location"]["lat"].Value<double>();
                toAdd.Longitude = locationJson["results"][0]["geometry"]["location"]["lng"].Value<double>();

                cities.Add(toAdd);
            }

            return cities;
        }



        private static JObject GetLocationJson(string cityName)
        {
            string url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var serializer = new JsonSerializer();

                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    JObject json = (JObject)serializer.Deserialize(jsonTextReader);
                    return json;
                }
            }

        }

        public static double GetCostBetweenTwoCities(City origin, City destination)
        {

            string url =
                $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(content);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
            double result = Convert.ToDouble(node.InnerText);

            return result / 100;
        }

        public static int GetDurationBetweenTwoCitiesByTollRoad(City origin, City destination)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            var json = JObject.Parse(content);

            try
            {
                return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
            }
            catch (Exception ex)
            {
                return -1;
            }
            
        }

        public static int GetDurationBetweenTwoCitiesByFreeRoad(City origin, City destination)
        {

            string url =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";
 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }

            var json = JObject.Parse(content);

            return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();

        }
    }
}
