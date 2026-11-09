using AutoFixture;
using AutoFixture.Kernel;

namespace SolTechnology.Core.Testing.Customizations;

/// <summary>
/// Produces <see cref="DateOnly"/> specimens by projecting a generated <see cref="DateTime"/>,
/// which AutoFixture cannot construct on its own.
/// </summary>
public sealed class DateOnlyGenerator : ISpecimenBuilder
{
    /// <summary>Returns a <see cref="DateOnly"/> for a <see cref="DateOnly"/> request, otherwise <see cref="NoSpecimen"/>.</summary>
    public object Create(object request, ISpecimenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request is not Type t || t != typeof(DateOnly))
        {
            return new NoSpecimen();
        }

        var dateTime = context.Create<DateTime>();
        return DateOnly.FromDateTime(dateTime);
    }
}

/// <summary>Registers <see cref="DateOnlyGenerator"/> at the front of the customization chain.</summary>
public sealed class DateOnlyCustomization : ICustomization
{
    /// <summary>Adds <see cref="DateOnlyGenerator"/> in front of the fixture's other customizations.</summary>
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Insert(0, new DateOnlyGenerator());
    }
}


