using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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

        public JObject DownloadLocationOfCity(string cityName)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={cityName}&key=AIzaSyBgCjCJuGQsXlAz6BUXPIL2_RSxgXUaCcM";

                Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
                getAsync.Wait();

                using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);
                        return json;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot find city {cityName}");
            }
        }

        public string DowloadCostBetweenTwoCities(City origin, City destination)
        {
            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

                Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
                getAsync.Wait();

                string content;
                using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }

                return content;
            }
            catch (Exception)
            {
                throw new InvalidDataException(
                    $"Cannot get data about cost between {origin.Name} and {destination.Name}");
            }
        }

        public JObject DowloadDurationBetweenTwoCitesByTollRoad(City origin, City destination)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
                getAsync.Wait();

                using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                        return json;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException(
                    $"Cannot get data about road between {origin.Name} and {destination.Name}");
            }
        }

        public JObject DowloadDurationBetweenTwoCitesByFreeRoad(City origin, City destination)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/distancematrix/json?units=imperial&origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&avoid=tolls&key=AIzaSyCdHbtbmF8Y2nfesiu0KUUJagdG7_oui1k";

                Task<HttpResponseMessage> getAsync = _httpClient.GetAsync(url);
                getAsync.Wait();

                using (Stream stream = getAsync.Result.Content.ReadAsStreamAsync().Result ??
                                       throw new ArgumentNullException(
                                           $"Execption on [{MethodBase.GetCurrentMethod().Name}]"))
                {
                    using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        JObject json = (JObject)new JsonSerializer().Deserialize(jsonTextReader);

                        return json;
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Cannot get data about road between {origin.Name} and {destination.Name}");
            }
        }
    }
}
