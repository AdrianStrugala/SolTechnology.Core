using Dapper;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository
{
    public partial class PlayerRepository : IPlayerRepository
    {

        private const string GetByIdSql = @"
  SELECT
    [ApiId],
	[Name],
	[DateOfBirth],
    [Nationality],
	[Position]
FROM [dbo].[Player]
WHERE [ApiId] = @PlayerApiId

SELECT 
       [PlayerApiId]
      ,[DateFrom]
      ,[DateTo]
      ,[Name]
  FROM [dbo].[Team]
  WHERE PlayerApiId = @PlayerApiId
";

        public Player GetById(int apiId)
        {
            Player result = null;

            var parameters = new
            {
                PlayerApiId = apiId,
            };

            if (_sqlConnectionFactory.HasOpenTransaction)
            {
                var transaction = _sqlConnectionFactory.GetTransaction();
                using var multi = transaction.Connection.QueryMultiple(GetByIdSql, parameters, transaction);
                result = multi.ReadSingleOrDefault<Player>();
                if (result != null)
                {
                    result.Teams = multi.Read<Team>().ToList();
                }
            }
            else
            {
                using var connection = _sqlConnectionFactory.CreateConnection();

                using var multi = connection.QueryMultiple(GetByIdSql, parameters);
                result = multi.ReadSingleOrDefault<Player>();
                if (result != null)
                {
                    result.Teams = multi.Read<Team>().ToList();
                }
            }
            return result;
        }
    }
}
