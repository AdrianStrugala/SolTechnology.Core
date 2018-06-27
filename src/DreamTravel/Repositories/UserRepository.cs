using System.Linq;
using System.Threading.Tasks;
using DapperExtensions;
using DreamTravel.Models;

namespace DreamTravel
{

    public class UserRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public UserRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task Add(User user)
        {
            using (var connection = _dbConnectionFactory.CreateDbConnection())
            {
                connection.Open();
                await connection.InsertAsync(user);
            }
        }

        public async Task<User> Get(string name)
        {
            User user;
            using (var connection = _dbConnectionFactory.CreateDbConnection())
            {
                connection.Open();

                user = (await connection.GetListAsync<User>(
                    Predicates.Field<User>(u => u.Name, Operator.Eq, name))).SingleOrDefault();
            }
            return user;
        }
    }
}
