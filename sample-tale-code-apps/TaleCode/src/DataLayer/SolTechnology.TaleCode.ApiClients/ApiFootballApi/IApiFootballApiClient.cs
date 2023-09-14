using SolTechnology.TaleCode.ApiClients.ApiFootballApi.Models;

namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi;

public interface IApiFootballApiClient
{
    Task<List<Team>> GetPlayerTeams(int apiId);
}