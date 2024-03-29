﻿using DreamTravel.Identity.DatabaseData.Repositories.Users;
using DreamTravel.Identity.Domain.Users;
using SolTechnology.Core.Sql.Testing;
using Xunit;

namespace DreamTravel.Identity.DatabaseData.IntegrationTests.Users
{
    public class UserRepositoryTests : IClassFixture<SqlFixture>
    {
        private readonly UserRepository _sut;

        public UserRepositoryTests(SqlFixture sqlFixture)
        {
            var connectionFactory = sqlFixture.SqlConnectionFactory;
            _sut = new UserRepository(connectionFactory);
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
