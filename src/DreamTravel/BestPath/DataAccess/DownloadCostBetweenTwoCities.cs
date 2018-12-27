using DreamTravel.BestPath.Interfaces;
using DreamTravel.SharedModels;
using System;
using System.IO;
using System.Net.Http;
using System.Xml;

namespace DreamTravel.BestPath.DataAccess
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DownloadCostBetweenTwoCities : IDownloadCostBetweenTwoCities
    {
        private readonly HttpClient _httpClient;

        public DownloadCostBetweenTwoCities()
        {
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
                throw new InvalidDataException(
                    $"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");
            }
        }


        public async Task<(double, double)> ExecuteV4(City origin, City destination)
        {
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
                throw new InvalidDataException(
                    $"Cannot get data about cost between [{origin.Name}] and [{destination.Name}]");
            }
        }


        public async Task<(double[], double[])> ExecuteV3(List<City> listOfCities)
        {
            double[] costMatrix = new double[listOfCities.Count * listOfCities.Count];
            double[] vinitaMatrix = new double[listOfCities.Count * listOfCities.Count];

            for (int i = 0; i < listOfCities.Count; i++)
            {
                for (int j = 0; j < listOfCities.Count; j++)
                {
                    try
                    {
                        string url =
                            $"http://apir.viamichelin.com/apir/1/route.xml/fra?steps=1:e:{listOfCities[i].Longitude}:{listOfCities[i].Latitude};1:e:{listOfCities[j].Longitude}:{listOfCities[j].Latitude}&authkey=JSBS20101202150903217741708195";

                        var response = await _httpClient.GetStringAsync(url);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response);

                        XmlNode node =
                                    doc.DocumentElement.SelectSingleNode(
                                        "/response/iti/header/summaries/summary/tollCost/car");
                        costMatrix[j + i * listOfCities.Count] = Convert.ToDouble(node.InnerText);

                        XmlNode vinietaNode =
                            doc.DocumentElement.SelectSingleNode(
                                "/response/iti/header/summaries/summary/CCZCost/car");
                        vinitaMatrix[j + i * listOfCities.Count] = Convert.ToDouble(vinietaNode.InnerText);
                    }

                    catch (Exception)
                    {
                        throw new InvalidDataException(
                            $"Cannot get data about cost between [{listOfCities[i].Name}] and [{listOfCities[j].Name}]");
                    }
                }
            }

            return (costMatrix, vinitaMatrix);
        }
    }
}
