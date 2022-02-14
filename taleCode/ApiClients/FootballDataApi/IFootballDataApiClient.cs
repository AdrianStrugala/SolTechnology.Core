namespace ApiClients.FootballDataApi
{
    public interface IFootballDataApiClient
    {
        public Task<SolTechnology.TaleCode.Domain.Player> GetPlayerById(int id);
    }

}