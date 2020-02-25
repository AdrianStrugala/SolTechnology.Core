using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using DreamTravel.Domain.Cities;
using Microsoft.Extensions.Logging;

namespace DreamTravel.GeolocationData.Query.DownloadRoadData.Clients
{
    public class DownloadCostBetweenTwoCities : IDownloadCostBetweenTwoCities
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DownloadCostBetweenTwoCities> _logger;

        public DownloadCostBetweenTwoCities(ILogger<DownloadCostBetweenTwoCities> logger)
        {
            _logger = logger;

            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
            }
        }

        public async Task<(double, double)> Execute(City origin, City destination)
        {
            if (origin.Name == destination.Name)
            {
                return (double.MaxValue, double.MaxValue);
            }

            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

                var response = await _httpClient.GetStringAsync(url);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(response);

                XmlNode node =
                    doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
                var tollCost = Convert.ToDouble(node.InnerText);

                XmlNode vinietaNode =
                    doc.DocumentElement.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
                var vinietaCost = Convert.ToDouble(vinietaNode.InnerText);


                return (tollCost / 100, vinietaCost / 100);
            }
            catch (Exception)
            {
                _logger.LogError($"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");

                return (-1, -1);
            }
        }
    }
}