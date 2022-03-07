namespace SolTechnology.TaleCode.ApiClients.ApiFootballApi;

public interface IApiFootballApiClient
{
    Task<List<string>> GetPlayerTeams(int apiId);
}