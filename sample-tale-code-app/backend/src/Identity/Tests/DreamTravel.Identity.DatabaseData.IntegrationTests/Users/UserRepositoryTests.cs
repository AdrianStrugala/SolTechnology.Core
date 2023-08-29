using DreamTravel.Domain.Users;
using DreamTravel.Identity.DatabaseData.IntegrationTests.TestsConfiguration;
using DreamTravel.Identity.DatabaseData.Repository.Users;
using DreamTravel.Infrastructure.Database;
using Xunit;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.Users
{
    public class UserRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly UserRepository _sut;
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepositoryTests(SqlFixture sqlFixture)
        {
            _connectionFactory = sqlFixture.DbConnectionFactory;
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
