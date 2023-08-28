using Dapper;
using DreamTravel.Domain.FlightEmailSubscriptions;

namespace DreamTravel.DatabaseData.Repository.SubscriptionDays
{
    public partial class SubscriptionDaysRepository : ISubscriptionDaysRepository
    {

        public void Update(Domain.FlightEmailSubscriptions.SubscriptionDays days)
        {
            string updateSql = @"
UPDATE [SubscriptionDays]
SET 
    [Monday] = @Monday,
    [Tuesday] = @Tuesday,
    [Wednesday] = @Wednesday,
    [Thursday] =  @Thursday,
    [Friday] = @Friday,
    [Saturday] =  @Saturday,
    [Sunday] = @Sunday
WHERE
    [FlightEmailSubscriptionId] = @FlightEmailSubscriptionId
";

            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                connection.Execute(updateSql, new
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
