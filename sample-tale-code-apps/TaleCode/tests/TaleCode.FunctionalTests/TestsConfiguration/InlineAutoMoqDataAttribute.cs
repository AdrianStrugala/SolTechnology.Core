using System.Diagnostics.CodeAnalysis;
using AutoFixture.Xunit2;

namespace TaleCode.FunctionalTests.TestsConfiguration;

[ExcludeFromCodeCoverage]
public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
{
    public InlineAutoMoqDataAttribute(params object[] objects) : base(new AutoFixtureDataAttribute(), objects) { }
}
