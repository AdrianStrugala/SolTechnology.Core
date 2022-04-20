
## Testing Piramide
## Unit tests



## Integration tests

Integration tests are made, as the name suggests, for integration. They may cover integration with APIs, external resources, storages and in specific - SQL database. SQL queries and commands often contain specific logic and mapping. In the case, all of the SQL operation should be properly tested. <b>SQL tests are covering business logic and this is why it's important to have them run in the build pipeline</b>.

<p>
Firstly, the correct configuration has to be setup:

```csharp
"ConnectionString": "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
```

Connection string points to local host on both: local and build (pipeline) environments.

The SQL Server is run in docker using following script:

```csharp
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=password_xxddd_2137' -p 1401:1433 --name DB -d mcr.microsoft.com/mssql/server:2019-latest
dotnet publish /p:TargetServerName=localhost /p:TargetPort=1401 /p:TargetUser=sa /p:TargetPassword=password_xxddd_2137 /p:TargetDatabaseName=TaleCodeDatabase (from 'taleCode/src/TaleCodeDatabase' directory)
```

As you can see, the two prerequisites are needed in this configuration:
- docker installed on the build machine
- SQL proj containing database schema, which creates .dacpac file on build

Alternatively, the SQL Server itself could be installed on the build machine - it may requrie app settings change before running integration tests.
</p>

<p>
When the SQL Server is up and running, the tests itself has to be configured as well:

- run on clean database
- run sequentially

The SqlFixture is intrduced to fulfill this needs:

```csharp
    public class SqlFixture : IAsyncLifetime
    {
        public ISqlConnectionFactory SqlConnectionFactory;
        public SqlConnection SqlConnection { get; private set; }
        private string _connectionString;

        public async Task InitializeAsync()
        {
            var config = Options.Create(new SqlConfiguration
            {
                //could be fetched from appSettings or environmental variable
                ConnectionString =
                    "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
            });

            _connectionString = config.Value.ConnectionString;

            SqlConnectionFactory = new SqlConnectionFactory(config);

            SqlConnection?.Dispose();
            SqlConnection = new SqlConnection(_connectionString);
            SqlConnection.Open();

            await new Respawn.Checkpoint().Reset(_connectionString);
        }

        public async Task DisposeAsync()
        {
            SqlConnection?.Dispose();
            await new Respawn.Checkpoint().Reset(_connectionString);
        }
```
</p>

<p>
And example test itself:

```csharp
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
```
</p>



## Functional Tests

## App Insights Logs