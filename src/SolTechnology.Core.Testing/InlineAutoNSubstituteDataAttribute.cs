using AutoFixture;
using AutoFixture.NUnit4;

namespace SolTechnology.Core.Testing;

/// <summary>
/// Inline counterpart of <see cref="AutoNSubstituteDataAttribute"/>: the leading positional values are
/// supplied explicitly and the remaining parameters are AutoFixture-generated with NSubstitute
/// auto-faking and <see cref="DateOnly"/> support.
/// </summary>
public class InlineAutoNSubstituteDataAttribute : InlineAutoDataAttribute
{
    /// <summary>Supplies <paramref name="values"/> for the leading parameters and generates the rest.</summary>
    public InlineAutoNSubstituteDataAttribute(params object[] values)
        : base(() => AutoNSubstituteDataAttribute.CreateFixture(Type.EmptyTypes), values)
    {
    }

    /// <summary>Lets a derived attribute supply its own fixture, for example one with extra customizations.</summary>
    protected InlineAutoNSubstituteDataAttribute(Func<IFixture> fixtureFactory, params object[] values)
        : base(fixtureFactory, values)
    {
    }
}


