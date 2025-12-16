using System.Xml;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DreamTravel.Trips.GeolocationDataClients.MichelinApi
{
    public class MichelinApiClient : IMichelinApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MichelinApiClient> _logger;
        private readonly MichelinApiOptions _options;

        public MichelinApiClient(IOptions<MichelinApiOptions> options, HttpClient httpClient, ILogger<MichelinApiClient> logger)
        {
            _logger = logger;
            _options = options.Value;
            _httpClient = httpClient;
        }

        public async Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination)
        {
            if (origin.Name == destination.Name)
            {
                return (double.MaxValue, double.MaxValue);
            }

            try
            {
                string url =
                    $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{origin.Longitude}:{origin.Latitude};1:e:{destination.Longitude}:{destination.Latitude}&authkey={_options.Key}";

                var response = await _httpClient.GetStringAsync(url);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(response);

                XmlNode node =
                    doc.DocumentElement?.SelectSingleNode("/response/iti/header/summaries/summary/tollCost/car");
                var tollCost = Convert.ToDouble(node?.InnerText);

                XmlNode vinietaNode =
                    doc.DocumentElement?.SelectSingleNode("/response/iti/header/summaries/summary/CCZCost/car");
                var vinietaCost = Convert.ToDouble(vinietaNode?.InnerText);


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