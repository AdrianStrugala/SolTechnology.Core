using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Core.Sql.Transactions;
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
        public void Inserting_Valid_Player_Saves_It_To_Database()
        {
            //Arrange

            var playerId = _fixture.Create<int>();

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
            var result = _sut.GetById(_fixture.Create<int>());


            //Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Not_Completed_Transaction_Is_Not_Modyfing_Database()
        {
            //Arrange

            var playerId = _fixture.Create<int>();

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

            var uow = new UnitOfWork();


            //Act
            using (uow.Begin())
            {
                _sut.Insert(player);

                // uow.Complete() is not called
            }


            //Assert
            var result = _sut.GetById(playerId);

            Assert.Null(result);
        }
    }
}