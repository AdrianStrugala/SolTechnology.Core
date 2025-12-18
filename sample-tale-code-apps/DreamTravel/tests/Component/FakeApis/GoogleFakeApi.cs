using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DreamTravel.Domain.Cities;
using DreamTravel.GeolocationDataClients.GoogleApi;
using SolTechnology.Core.Faker.FakesBase;
using WireMock.Matchers;
using WireMock.RequestBuilders;

namespace DreamTravel.FunctionalTests.FakeApis
{
    public class GoogleFakeApi : FakeApiBase<IGoogleApiClient>, IGoogleApiClient
    {
        protected override string BaseUrl => "google";


        public Task<TrafficMatrixResponse> GetSegmentDurationMatrixByTraffic(TrafficMatrixRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<City> GetLocationOfCity(string cityName)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/geocode/json"))
                .WithParam("address", $"{cityName}")
                .WithParam("key", "googleKey");

            Provider = BuildRequest(request);

            return default;
        }

        public Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities)
        {
            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/distancematrix/json"))
                .WithParam("units", "imperial")
                .WithParam("origins", $"{coordinates}")
                .WithParam("destinations", $"{coordinates}")
                .WithParam("key", "googleKey");

            Provider = BuildRequest(request);

            return default;
        }

        public Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities)
        {
            StringBuilder coordinates = new StringBuilder();
            foreach (City city in listOfCities)
            {
                coordinates.AppendFormat($"{city.Latitude},{city.Longitude}|");
            }

            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/distancematrix/json"))
                .WithParam("units", "imperial")
                .WithParam("origins", $"{coordinates}")
                .WithParam("destinations", $"{coordinates}")
                .WithParam("avoid", "tolls")
                .WithParam("key", "googleKey");

            Provider = BuildRequest(request);

            return default;
        }


        public Task<City> GetNameOfCity(City city)
        {
            throw new System.NotImplementedException();
        }

        internal static string TollDistanceMatrix = @"
{
   ""destination_addresses"" : 
   [
      ""Droga bez nazwy, 50-438 Wrocław, Poland"",
      ""P.za della Signoria, 16, 50122 Firenze FI, Italy"",
      ""Minoritenplatz 5, 1010 Wien, Austria"",
      ""C/ de Muntaner, 139, 08036 Barcelona, Spain""
   ],
   ""origin_addresses"" : 
   [
      ""Droga bez nazwy, 50-438 Wrocław, Poland"",
      ""P.za della Signoria, 16, 50122 Firenze FI, Italy"",
      ""Minoritenplatz 5, 1010 Wien, Austria"",
      ""C/ de Muntaner, 139, 08036 Barcelona, Spain""
   ],
   ""rows"" : 
   [
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""864 mi"",
                  ""value"" : 1390052
               },
               ""duration"" : 
               {
                  ""text"" : ""13 hours 57 mins"",
                  ""value"" : 50193
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""336 mi"",
                  ""value"" : 540565
               },
               ""duration"" : 
               {
                  ""text"" : ""5 hours 37 mins"",
                  ""value"" : 20220
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,244 mi"",
                  ""value"" : 2001261
               },
               ""duration"" : 
               {
                  ""text"" : ""19 hours 51 mins"",
                  ""value"" : 71459
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""865 mi"",
                  ""value"" : 1391959
               },
               ""duration"" : 
               {
                  ""text"" : ""14 hours 1 min"",
                  ""value"" : 50454
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""537 mi"",
                  ""value"" : 863662
               },
               ""duration"" : 
               {
                  ""text"" : ""8 hours 47 mins"",
                  ""value"" : 31608
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""676 mi"",
                  ""value"" : 1088260
               },
               ""duration"" : 
               {
                  ""text"" : ""11 hours 23 mins"",
                  ""value"" : 40982
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""335 mi"",
                  ""value"" : 538921
               },
               ""duration"" : 
               {
                  ""text"" : ""5 hours 41 mins"",
                  ""value"" : 20461
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""539 mi"",
                  ""value"" : 867277
               },
               ""duration"" : 
               {
                  ""text"" : ""8 hours 47 mins"",
                  ""value"" : 31597
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,107 mi"",
                  ""value"" : 1780836
               },
               ""duration"" : 
               {
                  ""text"" : ""18 hours 2 mins"",
                  ""value"" : 64925
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""1,244 mi"",
                  ""value"" : 2002210
               },
               ""duration"" : 
               {
                  ""text"" : ""19 hours 46 mins"",
                  ""value"" : 71157
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""684 mi"",
                  ""value"" : 1100142
               },
               ""duration"" : 
               {
                  ""text"" : ""11 hours 25 mins"",
                  ""value"" : 41083
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,118 mi"",
                  ""value"" : 1799561
               },
               ""duration"" : 
               {
                  ""text"" : ""17 hours 55 mins"",
                  ""value"" : 64526
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            }
         ]
      }
   ],
   ""status"" : ""OK""
}";


        internal static string FreeDistanceMatrix = @"
{
   ""destination_addresses"" : 
   [
      ""Droga bez nazwy, 50-438 Wrocław, Poland"",
      ""P.za della Signoria, 16, 50122 Firenze FI, Italy"",
      ""Minoritenplatz 5, 1010 Wien, Austria"",
      ""C/ de Muntaner, 139, 08036 Barcelona, Spain""
   ],
   ""origin_addresses"" : 
   [
      ""Droga bez nazwy, 50-438 Wrocław, Poland"",
      ""P.za della Signoria, 16, 50122 Firenze FI, Italy"",
      ""Minoritenplatz 5, 1010 Wien, Austria"",
      ""C/ de Muntaner, 139, 08036 Barcelona, Spain""
   ],
   ""rows"" : 
   [
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""866 mi"",
                  ""value"" : 1394314
               },
               ""duration"" : 
               {
                  ""text"" : ""18 hours 28 mins"",
                  ""value"" : 66480
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""242 mi"",
                  ""value"" : 388914
               },
               ""duration"" : 
               {
                  ""text"" : ""6 hours 46 mins"",
                  ""value"" : 24381
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,309 mi"",
                  ""value"" : 2107316
               },
               ""duration"" : 
               {
                  ""text"" : ""23 hours 49 mins"",
                  ""value"" : 85713
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""864 mi"",
                  ""value"" : 1391229
               },
               ""duration"" : 
               {
                  ""text"" : ""18 hours 32 mins"",
                  ""value"" : 66739
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""509 mi"",
                  ""value"" : 818437
               },
               ""duration"" : 
               {
                  ""text"" : ""14 hours 48 mins"",
                  ""value"" : 53279
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""766 mi"",
                  ""value"" : 1232538
               },
               ""duration"" : 
               {
                  ""text"" : ""20 hours 29 mins"",
                  ""value"" : 73765
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""242 mi"",
                  ""value"" : 390156
               },
               ""duration"" : 
               {
                  ""text"" : ""6 hours 45 mins"",
                  ""value"" : 24319
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""516 mi"",
                  ""value"" : 830266
               },
               ""duration"" : 
               {
                  ""text"" : ""14 hours 39 mins"",
                  ""value"" : 52732
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,270 mi"",
                  ""value"" : 2043640
               },
               ""duration"" : 
               {
                  ""text"" : ""1 day 1 hour"",
                  ""value"" : 88235
               },
               ""status"" : ""OK""
            }
         ]
      },
      {
         ""elements"" : 
         [
            {
               ""distance"" : 
               {
                  ""text"" : ""1,310 mi"",
                  ""value"" : 2109014
               },
               ""duration"" : 
               {
                  ""text"" : ""23 hours 45 mins"",
                  ""value"" : 85522
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""763 mi"",
                  ""value"" : 1227477
               },
               ""duration"" : 
               {
                  ""text"" : ""20 hours 27 mins"",
                  ""value"" : 73632
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1,267 mi"",
                  ""value"" : 2039683
               },
               ""duration"" : 
               {
                  ""text"" : ""1 day 0 hours"",
                  ""value"" : 88050
               },
               ""status"" : ""OK""
            },
            {
               ""distance"" : 
               {
                  ""text"" : ""1 ft"",
                  ""value"" : 0
               },
               ""duration"" : 
               {
                  ""text"" : ""1 min"",
                  ""value"" : 0
               },
               ""status"" : ""OK""
            }
         ]
      }
   ],
   ""status"" : ""OK""
}";

        public static string BuildGeocodingResponse(City city)
        {
            var countryCode = GetCountryCode(city.Country);

            return $@"{{
            ""results"": [
                {{
                    ""address_components"": [
                        {{
                            ""long_name"": ""{city.Name}"",
                            ""short_name"": ""{city.Name}"",
                            ""types"": [
                                ""locality"",
                                ""political""
                            ]
                        }},
                        {{
                            ""long_name"": ""{city.Country}"",
                            ""short_name"": ""{countryCode}"",
                            ""types"": [
                                ""country"",
                                ""political""
                            ]
                        }}
                    ],
                    ""formatted_address"": ""{city.Name}, {city.Country}"",
                    ""geometry"": {{
                        ""location"": {{
                            ""lat"": {city.Latitude},
                            ""lng"": {city.Longitude}
                        }},
                        ""location_type"": ""APPROXIMATE"",
                        ""viewport"": {{
                            ""northeast"": {{
                                ""lat"": {city.Latitude + 0.1},
                                ""lng"": {city.Longitude + 0.1}
                            }},
                            ""southwest"": {{
                                ""lat"": {city.Latitude - 0.1},
                                ""lng"": {city.Longitude - 0.1}
                            }}
                        }}
                    }},
                    ""place_id"": ""ChIJ{city.Name.GetHashCode():X}"",
                    ""types"": [
                        ""locality"",
                        ""political""
                    ]
                }}
            ],
            ""status"": ""OK""
        }}";
        }

        private static string GetCountryCode(string? country)
        {
            if (string.IsNullOrEmpty(country))
                return "XX";

            return country switch
            {
                "Poland" => "PL",
                "Italy" => "IT",
                "Austria" => "AT",
                "Spain" => "ES",
                "Germany" => "DE",
                "France" => "FR",
                "United States" => "US",
                "United Kingdom" => "GB",
                "Czech Republic" => "CZ",
                "Netherlands" => "NL",
                "Belgium" => "BE",
                "Switzerland" => "CH",
                "Portugal" => "PT",
                "Greece" => "GR",
                "Hungary" => "HU",
                "Slovakia" => "SK",
                _ => country.Length >= 2 ? country[..2].ToUpper() : "XX"
            };
        }
    }
}