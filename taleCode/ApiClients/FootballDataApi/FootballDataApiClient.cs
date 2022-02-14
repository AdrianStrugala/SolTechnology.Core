using SolTechnology.Core.ApiClient.Connection;

namespace ApiClients.FootballDataApi
{
    public class FootballDataApiClient : IFootballDataApiClient
    {
        private const string ApiName = "football-data"; //has to match configuration name
        private readonly IApiClientFactory _apiClientFactory;

        public FootballDataApiClient(IApiClientFactory apiClientFactory)
        {
            _apiClientFactory = apiClientFactory;
        }

        public async Task<SolTechnology.TaleCode.Domain.Player> GetPlayerById(int id)
        {
            var httpClient = _apiClientFactory.GetClient(ApiName);

            var result = await httpClient.GetAsync<PlayerModel>($"v2/players/{id}/matches");

            var domainResult = new SolTechnology.TaleCode.Domain.Player(
                result.Player.Id,
                result.Player.Name,
                result.Player.DateOfBirth,
                result.Player.Nationality,
                result.Player.Position,
                result.Matches.Select(m => new SolTechnology.TaleCode.Domain.Match(
                        m.Id,
                        result.Player.Id,
                        m.UtcDate,
                        m.HomeTeam.Name,
                        m.AwayTeam.Name,
                        m.Score.FullTime.HomeTeam,
                        m.Score.FullTime.AwayTeam,
                        m.Score.Winner))
                    .ToList());

            return domainResult;
        }
    }

}