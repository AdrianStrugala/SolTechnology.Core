﻿﻿using DreamTravel.Flows.SampleOrderWorkflow;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Core.Tale;
using SolTechnology.Core.Tale.Builder;

namespace DreamTravel.Flows;

public static class ModuleInstaller
{
    /// <summary>
    /// Registers DreamTravel stories. Returns the <see cref="ITaleBuilder"/> so the caller
    /// can pick a persistence provider (e.g. <c>.UseTaleRepository&lt;SQLiteTaleRepository&gt;()</c>
    /// from <c>DreamTravel.SQLite</c>). Defaults to in-memory persistence — sufficient for dev/tests.
    /// </summary>
    public static ITaleBuilder AddFlows(
        this IServiceCollection services,
        Action<TaleOptions>? configure = null)
        // Explicit assembly: GetCallingAssembly() is unreliable under JIT inlining / WAF.
        => services.AddSolTale(configure, typeof(SampleOrderWorkflowTale).Assembly);
}





