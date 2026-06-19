using AutoFixture.Kernel;

namespace SolTechnology.Core.Testing.Customizations;

/// <summary>
/// Specimen builder that ensures every generated <see cref="DateTime"/> has
/// <see cref="DateTimeKind.Utc"/>. Prevents tests from accidentally depending on local time.
/// </summary>
public sealed class UtcDateTimeSpecimen : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(DateTime))
        {
            return new NoSpecimen();
        }

        return DateTime.UtcNow.AddSeconds(Random.Shared.Next(-100_000, 100_000));
    }
}

