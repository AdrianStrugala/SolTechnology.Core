
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;

namespace SolTechnology.TaleCode.ApiClients.FootballDataApi
{
    public interface IFootballDataApiClient
    {
        public Task<FootballDataPlayer> GetPlayerById(int id);
        public Task<FootballDataMatch> GetMatchById(int matchApiId);
    }

}