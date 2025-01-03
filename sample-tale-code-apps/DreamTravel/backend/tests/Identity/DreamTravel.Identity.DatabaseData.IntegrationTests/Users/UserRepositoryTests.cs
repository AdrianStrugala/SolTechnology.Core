using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Sql.Testing;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.Users
{
    public class UserRepositoryTests
    {
        private UserRepository _sut;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var connectionFactory = IntegrationTestsFixture.SqlFixture.SqlConnectionFactory;
            _sut = new UserRepository(connectionFactory);
        }
        
        [SetUp]
        public async Task SetUp()
        {
            await IntegrationTestsFixture.SqlFixture.Reset();
        }

        [Test]
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

            resultUser.Should().NotBeNull();
            resultUser.Email.Should().Be(user.Email);
            resultUser.Name.Should().Be(user.Name);
            resultUser.Password.Should().Be(user.Password);
            resultUser.UserId.Should().Be(user.UserId);
        }
    }
}
