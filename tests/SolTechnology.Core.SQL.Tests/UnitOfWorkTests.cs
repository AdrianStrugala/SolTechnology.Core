using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.SQL.Connections;
using SolTechnology.Core.SQL.Transactions;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class UnitOfWorkTests
{
    [Test]
    public async Task Complete_ThroughInterface_AfterBegin_Commits()
    {
        // Arrange
        await using var provider = await SQLTestEnvironment.BuildProvider();
        var connectionFactory = provider.GetRequiredService<ISQLConnectionFactory>();
        var name = $"committed-{Guid.NewGuid():N}";

        // Act
        using (var unitOfWork = provider.GetRequiredService<IUnitOfWork>().Begin())
        {
            InsertItem(connectionFactory, name);
            unitOfWork.Complete();
        }

        // Assert
        (await SQLTestEnvironment.CountItems(name)).Should().Be(1);
    }

    [Test]
    public void Rollback_WithoutBegin_DoesNotThrow()
    {
        // Arrange
        using var provider = BuildProviderWithoutDatabase();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        // Act
        var rollback = () => unitOfWork.Rollback();

        // Assert
        rollback.Should().NotThrow();
    }

    [Test]
    public void Begin_Twice_Throws()
    {
        // Arrange
        using var provider = BuildProviderWithoutDatabase();
        using var unitOfWork = provider.GetRequiredService<IUnitOfWork>().Begin();

        // Act
        var beginAgain = () => unitOfWork.Begin();

        // Assert
        beginAgain.Should().Throw<InvalidOperationException>();
    }

    private static void InsertItem(ISQLConnectionFactory connectionFactory, string name)
    {
        using var connection = (SqlConnection)connectionFactory.CreateConnection();
        using var command = new SqlCommand("INSERT INTO dbo.Items (Name) VALUES (@name)", connection);
        command.Parameters.AddWithValue("@name", name);
        command.ExecuteNonQuery();
    }

    private static ServiceProvider BuildProviderWithoutDatabase() =>
        new ServiceCollection()
            .AddSolSQL(new SQLConfiguration { ConnectionString = "Server=localhost,1;Database=Unused" })
            .BuildServiceProvider();
}
