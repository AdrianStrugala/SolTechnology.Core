using System.Text.Json;
using FluentAssertions;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.CQRS.Tests.Errors;

[TestFixture]
public class ErrorSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public void Serialize_NotFoundError_IncludesDiscriminator()
    {
        // Arrange
        Error error = new NotFoundError { Message = "User not found" };

        // Act
        var json = JsonSerializer.Serialize(error, Options);

        // Assert
        json.Should().Contain("\"$type\":\"notFound\"");
    }

    [Test]
    public void RoundTrip_ValidationError_PreservesSubtype()
    {
        // Arrange
        Error error = new ValidationError
        {
            Message = "Validation failed",
            Errors = new Dictionary<string, string[]>
            {
                ["Name"] = new[] { "Required" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(error, Options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, Options);

        // Assert
        deserialized.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)deserialized!;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().Contain("Required");
    }

    [Test]
    public void RoundTrip_ConflictError_PreservesSubtype()
    {
        // Arrange
        Error error = new ConflictError { Message = "Duplicate key" };

        // Act
        var json = JsonSerializer.Serialize(error, Options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, Options);

        // Assert
        deserialized.Should().BeOfType<ConflictError>();
        deserialized!.Message.Should().Be("Duplicate key");
    }

    [Test]
    public void RoundTrip_AggregateError_PreservesSubtype()
    {
        // Arrange
        Error error = new AggregateError(new Error[]
        {
            new NotFoundError { Message = "A" },
            new ConflictError { Message = "B" }
        });

        // Act
        var json = JsonSerializer.Serialize(error, Options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, Options);

        // Assert
        deserialized.Should().BeOfType<AggregateError>();
        deserialized!.Message.Should().Contain("A");
        deserialized.Message.Should().Contain("B");
    }

    [TestCase(typeof(NotFoundError), "notFound")]
    [TestCase(typeof(ConflictError), "conflict")]
    [TestCase(typeof(UnauthorizedError), "unauthorized")]
    [TestCase(typeof(ForbiddenError), "forbidden")]
    [TestCase(typeof(ValidationError), "validation")]
    public void Serialize_AllSubtypes_IncludeCorrectDiscriminator(Type errorType, string expectedDiscriminator)
    {
        // Arrange
        var error = (Error)Activator.CreateInstance(errorType)!;
        // Records need Message set
        error = error with { Message = "test" };

        // Act
        var json = JsonSerializer.Serialize(error, Options);

        // Assert
        json.Should().Contain($"\"$type\":\"{expectedDiscriminator}\"");
    }
}


