using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using Polly;

namespace SolTechnology.Core.Sql.Connection
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        private IDbTransaction _transaction;

        private static readonly Random Random = new();

        public SqlConnectionFactory(IOptions<SqlConfiguration> sqlConfiguration)
        {
            _connectionString = sqlConfiguration.Value.ConnectionString;
        }
        public string GetConnectionString()
        {
            return _connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);

            Policy.Handle<Exception>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)) // 3,9,27s
                               + TimeSpan.FromMilliseconds(Random.Next(1000))) //delay up to 1s
                .Execute(() =>
                  {
                      connection.Open();
                  });
            return connection;
        }


        //Transaction handling
        public bool HasOpenTransaction => _transaction != null;
        public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _transaction ??= CreateConnection().BeginTransaction(isolationLevel);
        }

        public IDbTransaction GetTransaction()
        {
            if (_transaction == null)
            {
                throw new NullReferenceException(
                    "The transaction is not created. Invoke CreateTransaction() before getting!");
            }

            return _transaction;
        }

        public void Commit()
        {
            if (_transaction == null)
            {
                throw new NullReferenceException(
                    "The transaction is not created. Invoke CreateTransaction() before commiting!");
            }

            _transaction.Commit();

            CloseTransaction();
        }

        public void Rollback()
        {
            if (_transaction == null)
            {
                throw new NullReferenceException(
                    "The transaction is not created. Invoke CreateTransaction() before rollback!");
            }

            _transaction.Rollback();

            CloseTransaction();
        }

        private void CloseTransaction()
        {
            _transaction.Dispose();
            _transaction.Connection?.Dispose();
            _transaction = null;
        }

    }
}
