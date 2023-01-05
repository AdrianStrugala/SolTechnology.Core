using Refit;
using SolTechnology.TaleCode.ApiClients.FootballDataApi.Models;

namespace SolTechnology.TaleCode.ApiClients.FootballDataApi
{
    public interface IFootballDataRefitClient
    {
        [Get("/v2/players/{id}/matches?limit=999")]
        Task<PlayerModel> GetPlayerById(
            [AliasAs("id")] int id);
    }
}
