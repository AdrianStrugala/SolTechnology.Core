using System.Collections.Generic;
using System.Linq;
using Dapper;
using DreamTravel.DatabaseData.Users;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.DatabaseDataTests.Users
{
    public class UserRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly UserRepository _sut;
        private IDbConnectionFactory _connectionFactory;

        public UserRepositoryTests(SqlFixture sqlFixture)
        {
            _connectionFactory = sqlFixture.DbConnectionFactory;
            _sut = new UserRepository(_connectionFactory);
        }

        [Fact]
        public void Insert_ValidUser_ItIsSavedInDB()
        {
            //Arrange
            User user = new User();
            user.Name = "Adrian";
            user.Password = "Admin123";
            user.Email = "dreamtravel@gmail.com";

            //Act
            _sut.Insert(user);

            //Assert
            List<User> users = new List<User>();

            using (var connection = _connectionFactory.CreateConnection())
            {
                users = connection.Query<User>("SELECT * FROM [User]").ToList();
            }

            Assert.NotNull(users);
            Assert.NotEmpty(users);

            Assert.Equal(user.Email, users.First().Email);
            Assert.Equal(user.Name, users.First().Name);
            Assert.Equal(user.Password, users.First().Password);
        }
    }
}
