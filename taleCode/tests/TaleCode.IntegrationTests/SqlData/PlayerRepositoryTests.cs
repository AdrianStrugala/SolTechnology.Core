﻿using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.SqlData.Repository.PlayerRepository;
using Xunit;

namespace TaleCode.IntegrationTests.SqlData
{
    [Collection(nameof(TestsCollections.SqlTestsCollection))]
    public class PlayerRepositoryTests
    {
        private readonly PlayerRepository _sut;
        private readonly Fixture _fixture;

        public PlayerRepositoryTests(SqlFixture sqlFixture)
        {
            _fixture = new Fixture();
            _sut = new PlayerRepository(sqlFixture.SqlConnectionFactory);
        }

        [Fact]
        public void Insert_ValidPlayer_ItIsSavedInDB()
        {
            //Arrange

            var playerId = 123;

            var teams = _fixture.Build<Team>()
                .With(t => t.PlayerApiId, playerId)
                .CreateMany()
                .ToList();

            Player player = _fixture
                .Build<Player>()
                .With(p => p.ApiId, playerId)
                .With(p => p.DateOfBirth, DateTime.UtcNow.Date)
                .With(p => p.Teams, teams)
                .Create();

            //Act
            _sut.Insert(player);

            //Assert
            var result = _sut.GetById(playerId);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Teams);

            result.DateOfBirth.Should().Be(player.DateOfBirth);
            result.Name.Should().Be(player.Name);
            result.Nationality.Should().Be(player.Nationality);
            result.Position.Should().Be(player.Position);
            result.Teams.Should().BeEquivalentTo(player.Teams, 
                config: options => options
                    .Excluding(a => a.Id)
                    .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1))).WhenTypeIs<DateTime>());
        }
    }
}