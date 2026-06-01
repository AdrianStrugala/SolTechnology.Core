using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit4;
using SolTechnology.Core.Testing.Customizations;

namespace SolTechnology.Core.Testing;

/// <summary>
/// NUnit data attribute that feeds test methods with AutoFixture-generated arguments, auto-faking
/// interface/abstract parameters with <b>NSubstitute</b> (per the repo anti-stack — Moq is forbidden).
/// Supports <see cref="DateOnly"/> out of the box. Pass extra <see cref="ICustomization"/> types
/// (e.g. <c>typeof(BogusCustomization)</c> for realistic string data) to opt in to more specimen
/// builders, or use <see cref="AutoBogusDataAttribute"/> for the Bogus shorthand.
/// </summary>
public class AutoNSubstituteDataAttribute(params Type[] customizations)
    : AutoDataAttribute(() => CreateFixture(customizations))
{
    internal static IFixture CreateFixture(IEnumerable<Type> customizations)
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
        fixture.Customize(new DateOnlyCustomization());

        foreach (var customization in customizations)
        {
            if (Activator.CreateInstance(customization) is ICustomization instance)
            {
                fixture.Customize(instance);
            }
        }

        return fixture;
    }
}


