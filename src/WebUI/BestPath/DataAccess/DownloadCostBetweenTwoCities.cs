namespace WebUI.BestPath.DataAccess
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Xml;
    using Interfaces;
    using Microsoft.Extensions.Logging;
    using SharedModels;

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

        public (double, double) Execute(City origin, City destination)
        {
            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey=JSBS20101202150903217741708195";

                var response = _httpClient.GetStringAsync(url).Result;
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
                throw new InvalidDataException(
                    $"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");
            }
        }
    }
}
