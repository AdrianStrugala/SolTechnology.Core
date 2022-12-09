using Dapper;

namespace SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository
{
    public partial class ExecutionErrorRepository
    {

        private const string GetByTypeSql = @"
  SELECT
    [ReferenceType],
	[ReferenceId],
	[Message],
    [Valid]
FROM [dbo].[ExecutionError]
WHERE [ReferenceType] = @ReferenceType
AND [Valid] = 0
";

        public List<ExecutionError> GetByReferenceType(ReferenceType referenceType)
        {
            List<ExecutionError> result = null;

            using (var connection = _sqlConnectionFactory.CreateConnection())
            {
                result = SqlMapper.Query<ExecutionError>(connection, GetByTypeSql, new
                {
                    ReferenceType = referenceType.ToString(),
                }).ToList();
            }

            return result;
        }
    }
}
