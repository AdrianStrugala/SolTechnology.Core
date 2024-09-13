using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.Xunit2;

namespace TaleCode.IntegrationTests.TestsConfiguration;

[ExcludeFromCodeCoverage]
public class AutoFixtureDataAttribute : AutoDataAttribute
{
    public AutoFixtureDataAttribute() : this(() => new DataFixture())
    {
    }

    protected AutoFixtureDataAttribute(Func<IFixture> factory) : base(factory)
    {
    }
}
