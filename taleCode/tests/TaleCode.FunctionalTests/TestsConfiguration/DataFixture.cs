using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;

namespace TaleCode.FunctionalTests.TestsConfiguration;

[ExcludeFromCodeCoverage]
public class DataFixture : Fixture
{
    public DataFixture()
    {
        Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Behaviors.Remove(b));
        Behaviors.Add(new OmitOnRecursionBehavior(2));
    }
}
