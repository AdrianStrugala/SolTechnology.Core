using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.SQL.Tests;

[TestFixture]
public class SqlErrorTranslatorTests
{
    [Test]
    public async Task Translate_UniqueViolation_DescriptionHasNoTableName()
    {
        // Arrange
        var name = $"duplicate-{Guid.NewGuid():N}";
        await SQLTestEnvironment.InsertItem(name);

        // Act
        var result = await SqlErrorTranslator.Execute(() => SQLTestEnvironment.InsertItem(name));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error!.Description.Should().NotBeNullOrWhiteSpace();
        result.Error.Description.Should().NotContainAny("Items", "UQ_Items_Name", name);
    }
}
