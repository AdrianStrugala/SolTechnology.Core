
using ApiClients.FootballDataApi.Models;

namespace ApiClients.FootballDataApi
{
    public interface IFootballDataApiClient
    {
        public Task<FootballDataPlayer> GetPlayerById(int id);
        public Task<FootballDataMatch> GetMatchById(int matchApiId);
    }

}