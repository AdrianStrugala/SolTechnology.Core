using NSubstitute;

namespace SolTechnology.Core.Testing.Substitutes;

/// <summary>
/// Concise alias for <c>Arg.Any&lt;CancellationToken&gt;()</c> — cuts visual noise in
/// NSubstitute mock setups.
/// <para>
/// Before: <c>service.DoWork(Arg.Any&lt;CancellationToken&gt;())</c><br/>
/// After:  <c>service.DoWork(Ct.Any)</c>
/// </para>
/// </summary>
public static class Ct
{
    /// <summary>
    /// Matches any <see cref="CancellationToken"/> in an NSubstitute argument matcher.
    /// </summary>
    public static CancellationToken Any => Arg.Any<CancellationToken>();
}

