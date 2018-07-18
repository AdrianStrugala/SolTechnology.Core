using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
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

        public async Task<City> DownloadLocationOfCity(string cityName)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                City toAdd = new City { Name = cityName };
               
                HttpResponseMessage getAsync = await _httpClient.GetAsync(url);

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        toAdd.Latitude = json["results"][0]["geometry"]["location"]["lat"].Value<double>();
                        toAdd.Longitude = json["results"][0]["geometry"]["location"]["lng"].Value<double>();
                        return toAdd;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city [{cityName}]");
            }
        }

        public double DowloadCostBetweenTwoCities(City origin, City destination)
        {
            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

                HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                double result;
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
                        double tollCost = Convert.ToDouble(node.InnerText);

                        XmlNode vinietaNode = doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
                        double vinietaCost = Convert.ToDouble(vinietaNode.InnerText);

                        result = tollCost + vinietaCost;
                    }
                }

                return result;
            }
            catch (Exception)
            {
                throw new InvalidDataException(
                    $"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");
            }
        }

        public int DowloadDurationBetweenTwoCitesByTollRoad(City origin, City destination)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException(
                    $"Cannot get data about road between [{origin.Name}] and [{destination.Name}]");
            }
        }

        public int DowloadDurationBetweenTwoCitesByFreeRoad(City origin, City destination)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                HttpResponseMessage getAsync = _httpClient.GetAsync(url).Result;

                using (Stream stream = getAsync.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        return json["rows"][0]["elements"][0]["duration"]["value"].Value<int>();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot get data about road between [{origin.Name}] and [{destination.Name}]");
            }
        }
    }
}
