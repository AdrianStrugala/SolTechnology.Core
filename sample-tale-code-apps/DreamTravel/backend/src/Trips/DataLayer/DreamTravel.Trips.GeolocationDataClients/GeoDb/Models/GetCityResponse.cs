using System.Collections.Generic;

namespace DreamTravel.GeolocationData.GeoDb.Models
{
    public class GetCityResponse
    {
        public List<CityDetails> Data { get; set; }
        public List<Link> Links { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class CityDetails
    {
        public int Id { get; set; }
        public string WikiDataId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public object RegionCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Population { get; set; }
    }

    public class Link
    {
        public string Rel { get; set; }
        public string Href { get; set; }
    }

    public class Metadata
    {
        public int CurrentOffset { get; set; }
        public int TotalCount { get; set; }
    }
}
