using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class SQLPackageDependencyTests
{
    [Test]
    public void MsalUiRequiredException_ReferencedByDacFx_Loads()
    {
        // Act
        var load = () => Type.GetType("Microsoft.Identity.Client.MsalUiRequiredException, Microsoft.Identity.Client", throwOnError: true);

        // Assert
        load.Should().NotThrow().Which.Should().NotBeNull();
    }
}
