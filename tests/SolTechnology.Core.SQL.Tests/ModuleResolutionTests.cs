using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SolTechnology.Core.SQL.Connections;
using SolTechnology.Core.SQL.HealthChecks;
using SolTechnology.Core.SQL.Transactions;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private const string UnreachableSQL = "Server=localhost,1;Database=ResolutionTest;User Id=sa;Password=unused;Connect Timeout=1";

    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolSQL_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolSQL(new SQLConfiguration { ConnectionString = UnreachableSQL });
        services.AddHealthChecks().AddSolSqlHealthCheck();
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var connectionFactory = scoped.GetRequiredService<ISQLConnectionFactory>();
        var unitOfWork = scoped.GetRequiredService<IUnitOfWork>();
        var options = scoped.GetRequiredService<IOptions<SQLConfiguration>>().Value;
        var healthCheck = scoped.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Single(registration => registration.Name == "sql")
            .Factory(scoped);

        // Assert
        connectionFactory.GetConnectionString().Should().Be(UnreachableSQL);
        unitOfWork.Should().NotBeNull();
        options.ConnectionString.Should().Be(UnreachableSQL);
        healthCheck.Should().NotBeNull();
    }
}
