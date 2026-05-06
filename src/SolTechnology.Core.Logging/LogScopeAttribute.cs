namespace SolTechnology.Core.Logging;

/// <summary>
/// Marks a property of a CQRS request (MediatR <c>IRequest&lt;&gt;</c>) whose value should be
/// added to the per-operation log scope automatically by the logging pipeline behavior.
/// Without any <see cref="LogScopeAttribute"/> the request is still tracked
/// (START / SUCCESS / FAIL with duration) — only properties marked with this attribute
/// are projected into the scope, so PII fields are off by default.
/// </summary>
/// <example>
/// <code>
/// public sealed class FindCityByNameQuery : IRequest&lt;Result&lt;City&gt;&gt;
/// {
///     [LogScope]                       // -&gt; scope["Name"] = value
///     public string Name { get; set; } = null!;
/// }
///
/// public sealed class CreateTripCommand : IRequest&lt;Result&lt;int&gt;&gt;
/// {
///     [LogScope("Origin")]             // -&gt; scope["Origin"] = value (renamed)
///     public string OriginCity { get; set; } = null!;
///
///     public string CreditCardNumber { get; set; } = null!;   // not logged (no attribute)
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class LogScopeAttribute : Attribute
{
    /// <summary>
    /// Optional override of the scope-property key. Defaults to the property name when omitted.
    /// Use to publish a domain-friendly key (e.g. <c>OriginCity</c> property → <c>Origin</c> scope key).
    /// Convention: <c>PascalCase</c>.
    /// </summary>
    public string? Name { get; }

    public LogScopeAttribute() { }

    public LogScopeAttribute(string name)
    {
        Name = name;
    }
}

