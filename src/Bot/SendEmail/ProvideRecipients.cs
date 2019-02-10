namespace DreamTravel.Bot.SendEmail
{
    using Configuration;
    using Dapper;
    using Interfaces;
    using System.Collections.Generic;
    using System.Linq;

    public class ProvideRecipients : IProvideRecipients
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        private const string sql = @"
SELECT [Id]
      ,[Name]
      ,[Email]
  FROM [dbo].[Recipients]
";

        public ProvideRecipients(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public List<Recipient> Execute()
        {
            var result = new List<Recipient>();

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                result = connection.Query<Recipient>(sql).ToList();
            }
            
            return result;
        }
    }
}
