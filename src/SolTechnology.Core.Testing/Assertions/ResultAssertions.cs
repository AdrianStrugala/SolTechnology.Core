using FluentAssertions;
using FluentAssertions.Execution;

namespace SolTechnology.Core.Testing.Assertions;

/// <summary>
/// Fluent assertion extensions for <see cref="Result"/> and <see cref="Result{T}"/> that surface
/// <see cref="Error.Message"/> (and <see cref="Error.Description"/>) on failure — instead of the
/// bare "expected true but was false" you get with <c>result.IsSuccess.Should().BeTrue()</c>.
/// </summary>
public static class ResultAssertions
{
    /// <summary>
    /// Asserts the result is successful. On failure, the assertion message includes the
    /// <see cref="Error"/> details so the test output is self-explaining.
    /// </summary>
    public static void ShouldBeSuccess(this Result result, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result.IsSuccess)
            .FailWith("Expected Result to be successful{reason}, but it failed with: {0}",
                FormatError(result.Error));
    }

    /// <summary>
    /// Asserts the result is successful and returns the unwrapped <typeparamref name="T"/> for
    /// fluent chaining — e.g. <c>result.ShouldBeSuccess().Should().Be(42)</c>.
    /// </summary>
    public static T ShouldBeSuccess<T>(this Result<T> result, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result.IsSuccess)
            .FailWith("Expected Result<{0}> to be successful{reason}, but it failed with: {1}",
                typeof(T).Name, FormatError(result.Error));

        return result.Data!;
    }

    /// <summary>
    /// Asserts the result is a failure and returns the <see cref="Error"/> for further assertions.
    /// </summary>
    public static Error ShouldBeFailure(this Result result, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(result.IsFailure)
            .FailWith("Expected Result to be a failure{reason}, but it was successful.");

        return result.Error!;
    }

    /// <summary>
    /// Asserts the result is a failure with an error of the expected type and returns it
    /// strongly-typed — e.g. <c>result.ShouldBeFailure&lt;NotFoundError&gt;().Message.Should().Contain("42")</c>.
    /// </summary>
    public static TError ShouldBeFailure<TError>(this Result result, string because = "", params object[] becauseArgs)
        where TError : Error
    {
        var error = result.ShouldBeFailure(because, becauseArgs);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(error is TError)
            .FailWith("Expected error to be of type {0}{reason}, but got {1}: {2}",
                typeof(TError).Name, error.GetType().Name, FormatError(error));

        return (TError)error;
    }

    private static string FormatError(Error? error)
    {
        if (error is null) return "<null error>";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(error.Message))
            parts.Add(string.Concat("Message=\"", error.Message, "\""));

        if (!string.IsNullOrEmpty(error.Description))
            parts.Add(string.Concat("Description=\"", error.Description, "\""));

        parts.Add(string.Concat("Type=", error.GetType().Name));
        parts.Add(string.Concat("Recoverable=", error.Recoverable.ToString()));

        return string.Join(", ", parts);
    }
}


