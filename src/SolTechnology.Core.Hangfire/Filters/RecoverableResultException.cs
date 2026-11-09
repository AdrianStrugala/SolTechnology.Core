namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>
/// The exception of the <see cref="global::Hangfire.States.FailedState"/> that <see cref="SmartRetryJobFilter"/>
/// elects for a recoverable failed <see cref="Result"/>, so a retry filter can tell it from a thrown exception.
/// </summary>
internal sealed class RecoverableResultException(string message) : InvalidOperationException(message);
