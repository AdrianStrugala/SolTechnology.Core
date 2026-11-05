using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Core.SQL.Connections;
using SolTechnology.Core.SQL.Transactions;
using System.Runtime.CompilerServices;
using System.Transactions;

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
    public async Task Dispose_WithoutComplete_RollsBack()
    {
        // Arrange
        await using var provider = await SQLTestEnvironment.BuildProvider();
        var connectionFactory = provider.GetRequiredService<ISQLConnectionFactory>();
        var name = $"rolled-back-{Guid.NewGuid():N}";

        // Act
        using (provider.GetRequiredService<IUnitOfWork>().Begin())
        {
            InsertItem(connectionFactory, name);
        }

        // Assert
        (await SQLTestEnvironment.CountItems(name)).Should().Be(0);
    }

    [Test]
    public async Task Complete_AfterAwait_Commits()
    {
        // Arrange
        await using var provider = await SQLTestEnvironment.BuildProvider();
        var connectionFactory = provider.GetRequiredService<ISQLConnectionFactory>();
        var name = $"after-await-{Guid.NewGuid():N}";

        // Act
        using (var unitOfWork = provider.GetRequiredService<IUnitOfWork>().Begin())
        {
            await ResumeOnNewThread();
            InsertItem(connectionFactory, name);
            unitOfWork.Complete();
        }

        // Assert
        (await SQLTestEnvironment.CountItems(name)).Should().Be(1);
    }

    [Test]
    public void Complete_CommitFails_LeavesUnitOfWorkReusable()
    {
        // Arrange
        using var provider = BuildProviderWithoutDatabase();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        unitOfWork.Begin();
        Transaction.Current!.Rollback();

        // Act
        var complete = () => unitOfWork.Complete();

        // Assert
        complete.Should().Throw<TransactionAbortedException>();
        var beginAgain = () => unitOfWork.Begin().Dispose();
        beginAgain.Should().NotThrow();
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

    // Task.Yield can resume on the same thread, where even a thread-bound scope would still be current.
    private static ConfiguredTaskAwaitable ResumeOnNewThread() =>
        Task.Factory.StartNew(() => Thread.Sleep(50), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
            .ConfigureAwait(false);

    private static ServiceProvider BuildProviderWithoutDatabase() =>
        new ServiceCollection()
            .AddSolSQL(new SQLConfiguration { ConnectionString = "Server=localhost,1;Database=Unused" })
            .BuildServiceProvider();
}
