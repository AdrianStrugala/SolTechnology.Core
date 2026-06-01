using AutoFixture;
using AutoFixture.Kernel;

namespace SolTechnology.Core.Testing.Customizations;

/// <summary>
/// Produces <see cref="DateOnly"/> specimens by projecting a generated <see cref="DateTime"/>,
/// which AutoFixture cannot construct on its own.
/// </summary>
public sealed class DateOnlyGenerator : ISpecimenBuilder
{
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
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Insert(0, new DateOnlyGenerator());
    }
}


