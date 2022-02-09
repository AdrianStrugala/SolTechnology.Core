using ApiClients.FootballDataApi;
using Newtonsoft.Json;
using SolTechnology.Core.ApiClient.Connection;
using SolTechnology.TaleCode.Domain;

namespace ApiClients
{
    public class FootballDataApiClient : IFootballDataApiClient
    {
        private const string ApiName = "football-data"; //has to match configuration name
        private readonly IApiClientFactory _apiClientFactory;

        public FootballDataApiClient(IApiClientFactory apiClientFactory)
        {
            _apiClientFactory = apiClientFactory;
        }

        public async Task<Player> GetPlayerById(int id)
        {
            var httpClient = _apiClientFactory.GetClient(ApiName);

            HttpResponseMessage result = await httpClient.GetAsync($"v2/players/{id}/matches");

            var x = await result.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<PlayersMatchesModel>(x);

            return res?.Player;
        }
    }

}