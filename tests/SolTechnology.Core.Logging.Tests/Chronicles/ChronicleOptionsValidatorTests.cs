using FluentAssertions;
using NUnit.Framework;
using SolTechnology.Core.Logging.Chronicles;

namespace SolTechnology.Core.Logging.Tests.Chronicles;

public class ChronicleOptionsValidatorTests
{
    private readonly ChronicleOptionsValidator _sut = new();

    private static IEnumerable<TestCaseData> Bounds()
    {
        yield return Row("Defaults", _ => { }, true);
        yield return Row("MaxSteps=1", o => o.MaxSteps = 1, true);
        yield return Row("MaxSteps=10000", o => o.MaxSteps = 10_000, true);
        yield return Row("MaxSteps=0", o => o.MaxSteps = 0, false);
        yield return Row("MaxSteps=10001", o => o.MaxSteps = 10_001, false);
        yield return Row("MaxDepth=1", o => o.MaxDepth = 1, true);
        yield return Row("MaxDepth=64", o => o.MaxDepth = 64, true);
        yield return Row("MaxDepth=0", o => o.MaxDepth = 0, false);
        yield return Row("MaxDepth=65", o => o.MaxDepth = 65, false);
        yield return Row("MaxAttributesPerStep=0", o => o.MaxAttributesPerStep = 0, true);
        yield return Row("MaxAttributesPerStep=64", o => o.MaxAttributesPerStep = 64, true);
        yield return Row("MaxAttributesPerStep=-1", o => o.MaxAttributesPerStep = -1, false);
        yield return Row("MaxAttributesPerStep=65", o => o.MaxAttributesPerStep = 65, false);
        yield return Row("MaxAttributeValueLength=16", o => o.MaxAttributeValueLength = 16, true);
        yield return Row("MaxAttributeValueLength=4096", o => o.MaxAttributeValueLength = 4096, true);
        yield return Row("MaxAttributeValueLength=15", o => o.MaxAttributeValueLength = 15, false);
        yield return Row("MaxAttributeValueLength=4097", o => o.MaxAttributeValueLength = 4097, false);
        yield return Row("AggregateThreshold=2", o => (o.AggregateThreshold, o.NPlusOneThreshold) = (2, 2), true);
        yield return Row("AggregateThreshold=1", o => o.AggregateThreshold = 1, false);
        yield return Row("NPlusOneThreshold=AggregateThreshold", o => o.NPlusOneThreshold = o.AggregateThreshold, true);
        yield return Row("NPlusOneThreshold<AggregateThreshold", o => o.NPlusOneThreshold = o.AggregateThreshold - 1, false);
        yield return Row("SlowThreshold=1tick", o => o.SlowThreshold = TimeSpan.FromTicks(1), true);
        yield return Row("SlowThreshold=0", o => o.SlowThreshold = TimeSpan.Zero, false);
        yield return Row("SampleRate=0", o => o.SampleRate = 0.0, true);
        yield return Row("SampleRate=1", o => o.SampleRate = 1.0, true);
        yield return Row("SampleRate=-0.1", o => o.SampleRate = -0.1, false);
        yield return Row("SampleRate=1.1", o => o.SampleRate = 1.1, false);
    }

    [TestCaseSource(nameof(Bounds))]
    public void Validate_RangeBound_ReturnsExpectedOutcome(Action<ChronicleOptions> configure, bool isValid)
    {
        // Arrange
        var options = new ChronicleOptions();
        configure(options);

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.Should().Be(isValid);
        if (!isValid)
        {
            result.FailureMessage.Should().StartWith(nameof(ChronicleOptions));
        }
    }

    private static TestCaseData Row(string name, Action<ChronicleOptions> configure, bool isValid)
        => new TestCaseData(configure, isValid).SetArgDisplayNames(name);
}
