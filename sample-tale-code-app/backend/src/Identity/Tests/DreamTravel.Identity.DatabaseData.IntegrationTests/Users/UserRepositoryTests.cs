using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using DreamTravel.TestFixture.Sql;
using SolTechnology.Core.Sql.Connections;
using Xunit;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.Users
{
    public class UserRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly UserRepository _sut;
        private readonly ISqlConnectionFactory _connectionFactory;

        public UserRepositoryTests(SqlFixture sqlFixture)
        {
            _connectionFactory = sqlFixture.SqlConnectionFactory;
            _sut = new UserRepository(_connectionFactory);
        }

        [Fact]
        public void Insert_ValidUser_ItIsSavedInDB()
        {
            //Arrange
            User user = new User
            (
                name: "Adrian",
                password: "Admin123",
                email: "dreamtravel@gmail.com"
            );

            //Act
            _sut.Insert(user);

            //Assert

            User resultUser = _sut.Get(user.UserId);

            Assert.NotNull(resultUser);

            Assert.Equal(user.Email, resultUser.Email);
            Assert.Equal(user.Name, resultUser.Name);
            Assert.Equal(user.Password, resultUser.Password);
            Assert.Equal(user.UserId, resultUser.UserId);
        }
    }
}
