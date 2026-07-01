using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Tale.Builder;

/// <summary>Internal <see cref="ITaleBuilder"/> implementation.</summary>
internal sealed class TaleBuilder : ITaleBuilder
{
    public IServiceCollection Services { get; }
    public TaleOptions Options { get; }

    public TaleBuilder(IServiceCollection services, TaleOptions options)
    {
        Services = services;
        Options = options;
    }
}

