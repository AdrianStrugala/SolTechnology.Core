namespace DreamTravel.Bot.Configuration
{
    using System.Data;

    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
