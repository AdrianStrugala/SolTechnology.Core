using SolTechnology.Core.Testing.Customizations;

namespace SolTechnology.Core.Testing;

/// <summary>
/// Convenience NUnit data attribute: <see cref="AutoNSubstituteDataAttribute"/> with the
/// <see cref="BogusCustomization"/> already applied, so test parameters get realistic, member-aware
/// string data out of the box. Equivalent to <c>[AutoNSubstituteData(typeof(BogusCustomization))]</c>.
/// </summary>
public sealed class AutoBogusDataAttribute() : AutoNSubstituteDataAttribute(typeof(BogusCustomization));

