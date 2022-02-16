using ApiClients.FootballDataApi.Models;
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

        public async Task<FootballDataPlayer> GetPlayerById(int id)
        {
            var httpClient = _apiClientFactory.GetClient(ApiName);

            var apiResult = await httpClient.GetAsync<PlayerModel>($"v2/players/{id}/matches");

            var result = new FootballDataPlayer
            {
                Id = apiResult.Player.Id,
                Name = apiResult.Player.Name,
                DateOfBirth = apiResult.Player.DateOfBirth,
                Nationality = apiResult.Player.Nationality,
                Position = apiResult.Player.Position,
                Matches = apiResult.Matches.Select(m => new FootballDataMatch
                {
                    Id = m.Id,
                    Date = m.UtcDate,
                    HomeTeam = m.HomeTeam.Name,
                    AwayTeam = m.AwayTeam.Name,
                    HomeTeamScore = m.Score.FullTime.HomeTeam,
                    AwayTeamScore = m.Score.FullTime.AwayTeam,
                    Winner = m.Score.Winner
                }).ToList()
            };

            return result;
        }

        public async Task<FootballDataMatch> GetMatchById(int matchApiId)
        {
            var httpClient = _apiClientFactory.GetClient(ApiName);

            var apiResult = await httpClient.GetAsync<MatchModel>($"v2/matches/{matchApiId}");

            var result = new FootballDataMatch
            {
                Id = apiResult.Match.Id,
                Date = apiResult.Match.UtcDate,
                HomeTeam = apiResult.Match.HomeTeam.Name,
                AwayTeam = apiResult.Match.AwayTeam.Name,
                HomeTeamScore = apiResult.Match.Score.FullTime.HomeTeam,
                AwayTeamScore = apiResult.Match.Score.FullTime.AwayTeam,
                Winner = apiResult.Match.Score.Winner,
                CompetitionWinner = apiResult.Match.Season.Winner.Name
            };

            return result;
        }
    }

}