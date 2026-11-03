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

    private const string SnapshotTail = "\"message\":\"m\",\"description\":\"d\",\"recoverable\":true,\"correlationId\":\"c\"}";

    private static IEnumerable<TestCaseData> PersistedErrorSnapshots()
    {
        yield return new TestCaseData(new Error(), "{\"$type\":\"error\"," + SnapshotTail).SetName("Snapshot_error");
        yield return new TestCaseData(new NotFoundError(), "{\"$type\":\"notFound\"," + SnapshotTail).SetName("Snapshot_notFound");
        yield return new TestCaseData(new ConflictError(), "{\"$type\":\"conflict\"," + SnapshotTail).SetName("Snapshot_conflict");
        yield return new TestCaseData(new UnauthorizedError(), "{\"$type\":\"unauthorized\"," + SnapshotTail).SetName("Snapshot_unauthorized");
        yield return new TestCaseData(new ForbiddenError(), "{\"$type\":\"forbidden\"," + SnapshotTail).SetName("Snapshot_forbidden");
        yield return new TestCaseData(new TimeoutError(), "{\"$type\":\"timeout\"," + SnapshotTail).SetName("Snapshot_timeout");
        yield return new TestCaseData(new DeadlockError(), "{\"$type\":\"deadlock\"," + SnapshotTail).SetName("Snapshot_deadlock");
        yield return new TestCaseData(
                new ValidationError { Errors = new Dictionary<string, string[]> { ["Name"] = ["Required"] } },
                "{\"$type\":\"validation\",\"errors\":{\"Name\":[\"Required\"]}," + SnapshotTail)
            .SetName("Snapshot_validation");
        yield return new TestCaseData(
                new AggregateError([new NotFoundError { Message = "A" }]),
                "{\"$type\":\"aggregate\",\"innerErrors\":[{\"$type\":\"notFound\",\"message\":\"A\",\"description\":null,\"recoverable\":false,\"correlationId\":null}],\"message\":\"m\",\"description\":\"d\",\"recoverable\":true,\"correlationId\":\"c\"}")
            .SetName("Snapshot_aggregate");
    }

    [TestCaseSource(nameof(PersistedErrorSnapshots))]
    public void Serialize_PersistedDiscriminators_MatchSnapshot(Error error, string expectedJson)
    {
        // Arrange
        var populated = error with { Message = "m", Description = "d", Recoverable = true, CorrelationId = "c" };

        // Act
        var json = JsonSerializer.Serialize(populated, Options);

        // Assert
        json.Should().Be(expectedJson);
    }

    [Test]
    public void Serialize_UnknownSubtype_FallsBackToError()
    {
        // Arrange
        Error error = new CustomError { Message = "custom", Code = 42 };

        // Act
        var json = JsonSerializer.Serialize(error, Options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, Options);

        // Assert
        json.Should().Contain("\"$type\":\"error\"");
        json.Should().NotContain("\"code\"");
        deserialized.Should().BeOfType<Error>();
        deserialized!.Message.Should().Be("custom");
    }

    [Test]
    public void RoundTrip_AggregateError_PreservesInnerErrors()
    {
        // Arrange
        Error error = new AggregateError("Import failed.", new Error[]
        {
            new NotFoundError { Message = "A" },
            new ConflictError { Message = "B" }
        });

        // Act
        var json = JsonSerializer.Serialize(error, Options);
        var deserialized = JsonSerializer.Deserialize<Error>(json, Options);

        // Assert
        var aggregate = deserialized.Should().BeOfType<AggregateError>().Subject;
        aggregate.Message.Should().Be("Import failed. (A) (B)");
        aggregate.InnerErrors.Should().HaveCount(2);
        aggregate.InnerErrors[0].Should().BeOfType<NotFoundError>().Which.Message.Should().Be("A");
        aggregate.InnerErrors[1].Should().BeOfType<ConflictError>().Which.Message.Should().Be("B");
    }

    private sealed record CustomError : Error
    {
        public int Code { get; init; }
    }
}


