using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using DreamTravel.Domain.Cities;
using Microsoft.Extensions.Logging;

namespace DreamTravel.GeolocationData.Matrices
{
    public partial class MatrixRepository : IMatrixRepository
    {
        public async Task<(double[], double[])> GetCosts(List<City> cities)
        {
            int matrixSize = cities.Count * cities.Count;

            var result = (new double[matrixSize], new double[matrixSize]);

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < matrixSize; i++)
            {
                for (int j = 0; j < matrixSize; j++)
                {
                    int iterator = j + i * matrixSize;

                    var i1 = i;
                    var j1 = j;

                    (result.Item1[iterator], result.Item1[iterator]) = await DownloadCostBetweenTwoCities(cities[i1], cities[j1]);

                }
            }

            await Task.WhenAll(tasks);

            return result;
        }

        private async Task<(double, double)> DownloadCostBetweenTwoCities(City origin, City destination)
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
