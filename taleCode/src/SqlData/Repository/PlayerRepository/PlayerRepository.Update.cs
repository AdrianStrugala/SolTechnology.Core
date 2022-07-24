using Dapper;
using SolTechnology.TaleCode.Domain;

namespace SolTechnology.TaleCode.SqlData.Repository.PlayerRepository
{
    public partial class PlayerRepository : IPlayerRepository
    {
        //TODO: Add extension method to library (on match, on no match..., Match ON Columns... Insert, Update, Delete)

        private const string MergeTeamsSql = @"
  MERGE INTO [Team] AS [TARGET] 
            USING (
            VALUES
                (@PlayerApiId, @DateFrom, @DateTo, @Name)
            ) AS SOURCE (PlayerApiId, DateFrom, DateTo, [Name])
            ON SOURCE.PlayerApiId = [TARGET].PlayerApiId
			AND SOURCE.[Name] = [TARGET].[Name]
			AND SOURCE.DateFrom = [TARGET].DateFrom
            WHEN MATCHED THEN
            UPDATE SET [TARGET].DateTo = Source.DateTo
            WHEN NOT MATCHED THEN
            INSERT ([PlayerApiId], [DateFrom], [DateTo], [Name])
            VALUES (SOURCE.[PlayerApiId], SOURCE.[DateFrom], SOURCE.[DateTo], SOURCE.[Name]);
";

        public void Update(Player player)
        {
            using var connection = _sqlConnectionFactory.CreateConnection();
            connection.Execute(MergeTeamsSql, player.Teams);
        }
    }
}
