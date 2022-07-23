using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Core.Sql.Connection;
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
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public PlayerRepositoryTests(SqlFixture sqlFixture)
        {
            _fixture = new Fixture();
            _sqlConnectionFactory = sqlFixture.SqlConnectionFactory;
            _sut = new PlayerRepository(_sqlConnectionFactory);
        }

        [Fact]
        public void Inserting_Valid_Player_Saves_It_To_Database()
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


        [Fact]
        public void Getting_Not_Existing_Player_Returns_Null()
        {
            //Arrange


            //Act
            var result = _sut.GetById(420);


            //Assert
            result.Should().BeNull();
        }

        [Fact(Skip = "Not sure yet how to manage queries during open transaction")]
        
        public void Inserting_Players_In_Transaction_Makes_Db_Change_After_Commit()
        {
            //Arrange
            _sqlConnectionFactory.BeginTransaction();

            var playerId = 123;
            var player2Id = 456;

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

            Player player2 = _fixture
                .Build<Player>()
                .With(p => p.ApiId, player2Id)
                .With(p => p.DateOfBirth, DateTime.UtcNow.Date)
                .With(p => p.Teams, teams)
                .Create();

            //Act
            _sut.Insert(player);
            _sut.Insert(player2);


            //Assert before commit
            var shouldBeNull1 = _sut.GetById(playerId);
            var shouldBeNull2 = _sut.GetById(player2Id);

            Assert.Null(shouldBeNull1);
            Assert.Null(shouldBeNull2);


            //Act == commit
            _sqlConnectionFactory.Commit();


            //Assert after commit
            var result1 = _sut.GetById(playerId);
            var result2 = _sut.GetById(player2Id);

            Assert.NotNull(result1);
            Assert.NotEmpty(result1.Teams);

            Assert.NotNull(result2);
            Assert.NotEmpty(result2.Teams);
        }
    }
}