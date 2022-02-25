using Dapper;
using SolTechnology.Core.Sql.Connection;

namespace SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository
{
    public partial class ExecutionErrorRepository : IExecutionErrorRepository
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public ExecutionErrorRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }


        private const string InsertSql = @"
  INSERT INTO [dbo].[ExecutionError] (
    [ReferenceType],
    [ReferenceId],
	[Message],
    [Valid],
VALUES (@ReferenceType, @ReferenceId, @Message, @Valid)
";

        public void Insert(ExecutionError executionError)
        {
            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                connection.Execute(InsertSql, new
                {
                    ReferenceType = executionError.ReferenceType.ToString(),
                    ReferenceId = executionError.ReferenceId,
                    Message = executionError.Message,
                    Active = executionError.Valid
                });
            }
        }
    }
}
