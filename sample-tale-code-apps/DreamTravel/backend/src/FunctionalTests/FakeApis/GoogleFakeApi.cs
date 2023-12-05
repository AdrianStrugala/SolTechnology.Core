using System.Collections.Generic;
using System.Threading.Tasks;
using DreamTravel.GeolocationData.GoogleApi;
using DreamTravel.Trips.Domain.Cities;
using SolTechnology.Core.Faker.FakesBase;
using WireMock.Matchers;
using WireMock.RequestBuilders;

namespace DreamTravel.FunctionalTests.FakeApis
{
    public class GoogleFakeApi : FakeService<IGoogleApiClient>, IFakeApi, IGoogleApiClient
    {
        protected override string BaseUrl => "google";


        public Task<City> GetLocationOfCity(string cityName)
        {
            var request = Request
                .Create()
                .UsingGet()
                .WithPath(new WildcardMatcher($"/{BaseUrl}/maps/api/geocode/json"))
                .WithParam("address", $"{cityName}")
                .WithParam("key", "googleKey");

            Provider = BuildRequest(request);

            return null!;
        }

        public Task<double[]> GetDurationMatrixByTollRoad(List<City> listOfCities)
        {
            throw new System.NotImplementedException();
        }

        public Task<double[]> GetDurationMatrixByFreeRoad(List<City> listOfCities)
        {
            throw new System.NotImplementedException();
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
    }
}