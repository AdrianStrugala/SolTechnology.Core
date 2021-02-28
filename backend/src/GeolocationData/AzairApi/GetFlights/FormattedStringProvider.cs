using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DreamTravel.GeolocationData.AzairApi.GetFlights
{
    public static class FormattedStringProvider
    {
        public static string Month(DateTime dateTime)
        {
            string year = dateTime.Year.ToString("0000");
            string month = dateTime.Month.ToString("00");

            return year + month;
        }

        public static string Date(DateTime dateTime)
        {
            string year = dateTime.Year.ToString("0000");
            string month = dateTime.Month.ToString("00");
            string day = dateTime.Day.ToString("00");

            return year + "-" + month + "-" + day;
        }

        public static string Airports(List<string> airports)
        {
            if (airports.Count == 1)
            {
                return "%5B" + airports.First() + "%5D";
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("%5B");
                stringBuilder.Append(airports.First());
                stringBuilder.Append("%5D+%28%2B");
                for (int i = 1; i < airports.Count - 1; i++)
                {
                    stringBuilder.Append(airports[i]);
                    stringBuilder.Append("%2C");
                }

                stringBuilder.Append(airports.Last());
                stringBuilder.Append("%29");

                return stringBuilder.ToString();
            }
        }
    }
}
