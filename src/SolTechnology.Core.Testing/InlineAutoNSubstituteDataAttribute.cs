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
    public InlineAutoNSubstituteDataAttribute(params object[] values)
        : base(() => AutoNSubstituteDataAttribute.CreateFixture(Type.EmptyTypes), values)
    {
    }

    protected InlineAutoNSubstituteDataAttribute(Func<IFixture> fixtureFactory, params object[] values)
        : base(fixtureFactory, values)
    {
    }
}


