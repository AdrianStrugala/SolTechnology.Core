using AutoFixture;
using AutoFixture.Kernel;
using Bogus;

namespace SolTechnology.Core.Testing.Customizations;

/// <summary>
/// Registers a fully-controlled Bogus <see cref="Faker{T}"/> so AutoFixture produces <typeparamref name="T"/>
/// instances from your own rules. Use when the member-name heuristics of <see cref="BogusCustomization"/>
/// are not enough and you want explicit <c>RuleFor</c> control for a specific type.
/// </summary>
/// <example>
/// <code>
/// var customer = new Faker&lt;Customer&gt;()
///     .RuleFor(c => c.Iban, f => f.Finance.Iban())
///     .RuleFor(c => c.Name, f => f.Company.CompanyName());
///
/// fixture.Customize(new BogusCustomization&lt;Customer&gt;(customer));
/// // or, in one shot for a single test:
/// // [Test, AutoNSubstituteData] public void T(IFixture fixture) { fixture.Customize(new BogusCustomization&lt;Customer&gt;(customer)); ... }
/// </code>
/// </example>
public sealed class BogusCustomization<T> : ICustomization where T : class
{
    private readonly Faker<T> _faker;

    public BogusCustomization(Faker<T> faker) => _faker = faker ?? throw new ArgumentNullException(nameof(faker));

    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Insert(0, new FakerSpecimen<T>(_faker));
    }
}

internal sealed class FakerSpecimen<T>(Faker<T> faker) : ISpecimenBuilder where T : class
{
    public object Create(object request, ISpecimenContext context)
        => request is Type t && t == typeof(T)
            ? faker.Generate()
            : new NoSpecimen();
}

