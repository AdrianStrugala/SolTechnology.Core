using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Tests;

public sealed class AggregateErrorTests
{
    [Test]
    public void Message_WithoutInnerErrors_IsTheBaseMessage()
    {
        // Act
        var error = new AggregateError("Import failed.", []);

        // Assert
        error.Message.Should().Be("Import failed.");
        error.InnerErrors.Should().BeEmpty();
    }

    [Test]
    public void Message_WithInnerErrors_AppendsEachInnerMessage()
    {
        // Act
        var error = new AggregateError("Import failed.",
        [
            new NotFoundError { Message = "A" },
            new ConflictError { Message = "B" }
        ]);

        // Assert
        error.Message.Should().Be("Import failed. (A) (B)");
    }

    [Test]
    public void DefaultMessage_IsUsedWhenNoneIsGiven()
    {
        // Act
        var error = new AggregateError([new Error { Message = "A" }]);

        // Assert
        error.Message.Should().Be("One or more errors occurred. (A)");
    }
}
