using SolTechnology.TaleCode.Domain;

namespace ApiClients
{
    public interface IFootballDataApiClient
    {
        public  Task<Player> GetPlayerById(int id);
    }

}