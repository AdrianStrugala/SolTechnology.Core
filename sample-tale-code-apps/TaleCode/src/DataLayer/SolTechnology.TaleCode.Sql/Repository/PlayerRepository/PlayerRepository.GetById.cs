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

            using var connection = _sqlConnectionFactory.CreateConnection();
            using var multi = connection.QueryMultiple(GetByIdSql, new
            {
                PlayerApiId = apiId,
            });
            result = multi.ReadSingleOrDefault<Player>();
            if (result != null)
            {
                result.Teams = multi.Read<Team>().ToList();
            }

            return result;
        }
    }
}
