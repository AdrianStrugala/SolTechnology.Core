using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Core.Tale.Builder;

/// <summary>Internal <see cref="ITaleBuilder"/> implementation.</summary>
internal sealed class TaleBuilder(IServiceCollection services, TaleOptions options, TaleHandlerRegistry registry)
    : ITaleBuilder
{
    public IServiceCollection Services { get; } = services;
    public TaleOptions Options { get; } = options;

    public ITaleBuilder ExposeOverHttp<THandler>() where THandler : class
    {
        if (!ModuleInstaller.InheritsFromOpenGeneric(typeof(THandler), typeof(TaleHandler<,,>)))
        {
            throw new ArgumentException($"{typeof(THandler).Name} is not a TaleHandler.", nameof(THandler));
        }

        registry.Expose(typeof(THandler));
        return this;
    }
}

