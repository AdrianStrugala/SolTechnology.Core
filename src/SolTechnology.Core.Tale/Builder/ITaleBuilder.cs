using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Tale.Builder;

/// <summary>
/// Builder returned by <c>AddSolTale</c>. Persistence providers plug in through
/// extension methods on this type (<c>UseInMemoryTaleRepository</c>,
/// <c>UseTaleRepository&lt;T&gt;</c>). A repository is
/// always present — the default after <c>AddSolTale()</c> is in-memory. The builder
/// is a thin handle — its only state is the underlying <see cref="IServiceCollection"/>
/// and the <see cref="TaleOptions"/> already registered there.
/// </summary>
public interface ITaleBuilder
{
    /// <summary>The service collection being configured.</summary>
    IServiceCollection Services { get; }

    /// <summary>The singleton <see cref="TaleOptions"/> registered by <c>AddSolTale</c>.</summary>
    TaleOptions Options { get; }
}

