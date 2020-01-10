using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;
using DreamTravel.Infrastructure.Database;

namespace DreamTravel.DatabaseData.SubscriptionDays
{
    public class SubscriptionDaysRepository : ISubscriptionDaysRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public SubscriptionDaysRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public void Insert(Domain.FlightEmailSubscriptions.SubscriptionDays days)
        {
            string insertSql = @"
INSERT INTO [SubscriptionDays]
([FlightEmailSubscriptionId], [Monday], [Tuesday], [Wednesday], [Thursday], [Friday], [Saturday], [Sunday])
OUTPUT INSERTED.ID
VALUES
(@FlightEmailSubscriptionId, @Monday, @Tuesday, @Wednesday, @Thursday, @Friday, @Saturday, @Sunday)
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(insertSql, new
                {
                    FlightEmailSubscriptionId = days.FlightEmailSubscriptionId,
                    Monday = days.Monday,
                    Tuesday = days.Tuesday,
                    Wednesday = days.Wednesday,
                    Thursday = days.Thursday,
                    Friday = days.Friday,
                    Saturday = days.Saturday,
                    Sunday = days.Sunday
                });
            }
        }
    }
}
