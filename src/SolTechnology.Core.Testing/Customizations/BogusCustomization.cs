using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Bogus;

namespace SolTechnology.Core.Testing.Customizations;

/// <summary>
/// Routes AutoFixture's <see cref="string"/> generation through <b>Bogus</b> so generated specimens
/// carry realistic, member-aware values (a property called <c>Email</c> gets an email, <c>FirstName</c>
/// a first name, …) instead of GUID-like noise. Everything else still flows through AutoFixture.
/// </summary>
/// <remarks>
/// Plug it in either way:
/// <code>
/// [Test, AutoNSubstituteData(typeof(BogusCustomization))]   // via attribute
/// [Test, AutoBogusData]                                     // convenience attribute (same thing)
/// fixture.Customize(new BogusCustomization());              // or directly on a fixture
/// </code>
/// For deterministic, fully-controlled data use <see cref="BogusCustomization{T}"/> with your own
/// <c>Faker&lt;T&gt;</c> — it avoids Bogus' process-wide global seed.
/// </remarks>
public sealed class BogusCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Insert(0, new BogusStringSpecimen(new Faker()));
    }
}

/// <summary>
/// Member-name-aware string generator. When the request is a string property/parameter/field it maps
/// the member name to a matching Bogus dataset; bare <see cref="string"/> requests fall back to a word.
/// </summary>
internal sealed class BogusStringSpecimen(Faker faker) : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        var memberName = request switch
        {
            PropertyInfo p when p.PropertyType == typeof(string) => p.Name,
            ParameterInfo pi when pi.ParameterType == typeof(string) => pi.Name,
            FieldInfo f when f.FieldType == typeof(string) => f.Name,
            _ => null
        };

        if (memberName is null)
        {
            return request is Type t && t == typeof(string)
                ? faker.Lorem.Word()
                : new NoSpecimen();
        }

        return GenerateForMember(memberName);
    }

    private string GenerateForMember(string name)
    {
        var n = name.ToLowerInvariant();

        return n switch
        {
            _ when n.Contains("email") => faker.Internet.Email(),
            _ when n.Contains("username") || n.Contains("login") => faker.Internet.UserName(),
            _ when n.Contains("firstname") => faker.Name.FirstName(),
            _ when n.Contains("lastname") || n.Contains("surname") => faker.Name.LastName(),
            _ when n.Contains("phone") || n.Contains("mobile") => faker.Phone.PhoneNumber(),
            _ when n.Contains("street") || n.Contains("address") => faker.Address.StreetAddress(),
            _ when n.Contains("city") => faker.Address.City(),
            _ when n.Contains("country") => faker.Address.Country(),
            _ when n.Contains("zip") || n.Contains("postal") => faker.Address.ZipCode(),
            _ when n.Contains("company") || n.Contains("merchant") => faker.Company.CompanyName(),
            _ when n.Contains("url") || n.Contains("website") => faker.Internet.Url(),
            _ when n.Contains("currency") => faker.Finance.Currency().Code,
            _ when n.Contains("iban") => faker.Finance.Iban(),
            _ when n.Contains("bic") || n.Contains("swift") => faker.Finance.Bic(),
            _ when n.Contains("description") || n.Contains("comment") || n.Contains("note") => faker.Lorem.Sentence(),
            // Generic "…name" fallback runs last so specific names above win (e.g. userName).
            _ when n.Contains("fullname") || n == "name" || n.EndsWith("name") => faker.Name.FullName(),
            _ => faker.Lorem.Word()
        };
    }
}

