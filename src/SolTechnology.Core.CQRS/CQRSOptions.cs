namespace SolTechnology.Core.CQRS;

/// <summary>
/// Configuration options for the CQRS module.
/// </summary>
public sealed class CQRSOptions
{
    /// <summary>
    /// When true (default), registers <c>FluentValidationPipelineBehavior</c> and scans
    /// assemblies for <c>IValidator&lt;T&gt;</c> implementations.
    /// </summary>
    public bool UseFluentValidation { get; set; } = true;

    /// <summary>
    /// When true (default), registers <c>LoggingPipelineBehavior</c> for automatic
    /// operation tracking.
    /// </summary>
    public bool UseLogging { get; set; } = true;
}

