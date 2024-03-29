﻿using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;

namespace SolTechnology.TaleCode.ApiClients.FootballDataApi
{
    public class FootballDataApiClient : IFootballDataApiClient
    {
        private readonly HttpClient _httpClient;

        public FootballDataApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FootballDataPlayer> GetPlayerById(int id)
        {
            var apiResult = await _httpClient
                .CreateRequest($"v2/players/{id}/matches?limit=999")
                .GetAsync<PlayerModel>();

            var result = new FootballDataPlayer
            {
                Id = apiResult.Player.Id,
                Name = apiResult.Player.Name,
                DateOfBirth = apiResult.Player.DateOfBirth,
                Nationality = apiResult.Player.Nationality,
                Position = apiResult.Player.Position,
                Matches = apiResult.Matches.OrderBy(m => m.UtcDate).Select(m => new FootballDataMatch
                {
                    Id = m.Id,
                    Date = m.UtcDate,
                    HomeTeam = m.HomeTeam.Name,
                    AwayTeam = m.AwayTeam.Name,
                    HomeTeamScore = m.Score.FullTime.HomeTeam,
                    AwayTeamScore = m.Score.FullTime.AwayTeam,
                    Winner = GetWinner(m)
                }).ToList()
            };

            return result;
        }

        public async Task<FootballDataMatch> GetMatchById(int matchApiId)
        {
            var apiResult = await _httpClient
                .CreateRequest($"v2/matches/{matchApiId}")
                .GetAsync<MatchModel>();

            var result = new FootballDataMatch
            {
                Id = apiResult.Match.Id,
                Date = apiResult.Match.UtcDate,
                HomeTeam = apiResult.Match.HomeTeam.Name,
                AwayTeam = apiResult.Match.AwayTeam.Name,
                HomeTeamScore = apiResult.Match.Score.FullTime.HomeTeam,
                AwayTeamScore = apiResult.Match.Score.FullTime.AwayTeam,
                Winner = GetWinner(apiResult.Match),
                CompetitionWinner = apiResult.Match.Season.Winner?.Name
            };

            return result;
        }

        private string GetWinner(Match m)
        {
            return m.Score.Winner.Contains("HOME") ? m.HomeTeam.Name : m.AwayTeam.Name;
        }
    }

}