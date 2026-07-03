using SolTechnology.Core.Tale.Orchestration;

namespace SolTechnology.Core.Tale;

/// <summary>
/// A fluent plan for a tale — the chapters to read, in order, with guards and recovery — written so
/// <c>Tell()</c> reads like a table of contents. A Tale describes the flow; the <c>TaleEngine</c>
/// executes it. Like a railway, the tale runs on one of two tracks — <b>won</b> or <b>lost</b>.
/// Every step acts only on the won track; the first failure switches the tale to the lost track and
/// the remaining chapters are skipped, unless an <see cref="Otherwise{TChapter}()">Otherwise</see>
/// puts it back.
/// </summary>
/// <typeparam name="TContext">The context carrying state through the chapters.</typeparam>
/// <typeparam name="TOutput">The output produced when the tale concludes.</typeparam>
/// <example>
/// <code>
/// protected override Tale&lt;RouteResult&gt; Tell() =&gt;
///     Open&lt;LoadCities&gt;()
///         .Expect(ctx =&gt; ctx.Cities.Count &gt; 1,
///                 new NotFoundError { Message = "A route needs at least two cities." })
///         .Read&lt;DownloadRoadData&gt;()
///         .Read&lt;FindProfitablePath&gt;()
///         .Otherwise&lt;OrderByDistance&gt;()                 // no profitable path → fall back
///         .Read&lt;SolveTsp&gt;()
///         .WhenLost(error =&gt; logger.LogWarning("{Error}", error.Message))
///         .Read&lt;FormResult&gt;()
///         .Finale(ctx =&gt; ctx.Output);
/// </code>
/// </example>
public sealed class Tale<TContext, TOutput>
    where TContext : class
    where TOutput : class
{
    private readonly List<TaleStep> _steps;

    private Tale(List<TaleStep> steps) => _steps = steps;

    internal static Tale<TContext, TOutput> Open<TChapter>() where TChapter : IChapter<TContext>
        => new([new ReadStep(typeof(TChapter))]);

    internal static Tale<TContext, TOutput> Open(Action<TContext> step)
        => new([new InlineStep(ctx => { step((TContext)ctx); return Task.FromResult(Result.Success()); })]);

    internal static Tale<TContext, TOutput> Open(Func<TContext, Task<Result>> step)
        => new([new InlineStep(ctx => step((TContext)ctx))]);

    internal IReadOnlyList<TaleStep> Steps => _steps;

    /// <summary>Read the next chapter. Skipped if the tale has already switched to the lost track.</summary>
    /// <typeparam name="TChapter">The chapter to read; resolved from DI.</typeparam>
    public Tale<TContext, TOutput> Read<TChapter>() where TChapter : IChapter<TContext>
    {
        _steps.Add(new ReadStep(typeof(TChapter)));
        return this;
    }

    /// <summary>
    /// Stay on the won track only while <paramref name="condition"/> holds; otherwise switch to the
    /// lost track carrying <paramref name="otherwise"/>. The tale's precondition check.
    /// </summary>
    /// <param name="condition">What the context must satisfy to keep going.</param>
    /// <param name="otherwise">The error to fail with when the condition does not hold.</param>
    public Tale<TContext, TOutput> Expect(Func<TContext, bool> condition, Error otherwise)
    {
        _steps.Add(new GuardStep(ctx => condition((TContext)ctx), otherwise));
        return this;
    }

    /// <summary>
    /// Recover from a lost tale by reading a fallback chapter — clears the error and puts the tale
    /// back on the won track. Runs only when the tale has failed; ignored otherwise.
    /// </summary>
    /// <typeparam name="TChapter">The fallback chapter; resolved from DI.</typeparam>
    public Tale<TContext, TOutput> Otherwise<TChapter>() where TChapter : IChapter<TContext>
    {
        _steps.Add(new FallbackChapterStep(typeof(TChapter)));
        return this;
    }

    /// <summary>
    /// Recover from a lost tale with an inline fallback — clears the error and puts the tale back on
    /// the won track. Runs only when the tale has failed; ignored otherwise.
    /// </summary>
    /// <param name="recover">Inline recovery that mutates the context and returns its own result.</param>
    public Tale<TContext, TOutput> Otherwise(Func<TContext, Task<Result>> recover)
    {
        _steps.Add(new FallbackStep(ctx => recover((TContext)ctx)));
        return this;
    }

    /// <summary>Run a side-effect (logging, metrics, alerting) when the tale has been lost. Track unchanged.</summary>
    public Tale<TContext, TOutput> WhenLost(Action<Error> effect)
    {
        _steps.Add(new OnLostStep(error => { effect(error); return Task.CompletedTask; }));
        return this;
    }

    /// <summary>Asynchronous <see cref="WhenLost(Action{Error})"/>.</summary>
    public Tale<TContext, TOutput> WhenLost(Func<Error, Task> effect)
    {
        _steps.Add(new OnLostStep(effect));
        return this;
    }

    /// <summary>Run a side-effect when the tale is still being won. Track unchanged.</summary>
    public Tale<TContext, TOutput> WhenWon(Action<TContext> effect)
    {
        _steps.Add(new OnWonStep(ctx => { effect((TContext)ctx); return Task.CompletedTask; }));
        return this;
    }

    /// <summary>Asynchronous <see cref="WhenWon(Action{TContext})"/>.</summary>
    public Tale<TContext, TOutput> WhenWon(Func<TContext, Task> effect)
    {
        _steps.Add(new OnWonStep(ctx => effect((TContext)ctx)));
        return this;
    }

    /// <summary>An inline step — a small piece of logic that mutates the context without a dedicated chapter.</summary>
    public Tale<TContext, TOutput> Do(Action<TContext> step)
    {
        _steps.Add(new InlineStep(ctx => { step((TContext)ctx); return Task.FromResult(Result.Success()); }));
        return this;
    }

    /// <summary>An inline step that may itself fail, returning its own <see cref="Result"/>.</summary>
    public Tale<TContext, TOutput> Do(Func<TContext, Task<Result>> step)
    {
        _steps.Add(new InlineStep(ctx => step((TContext)ctx)));
        return this;
    }

    /// <summary>
    /// Conclude the tale: project the final context into the output. This seals the plan into a
    /// <see cref="Tale{TOutput}"/> that <c>Tell()</c> returns. Runs only when the tale is won — a
    /// lost or paused tale returns its error instead.
    /// </summary>
    /// <param name="conclusion">Builds the output from the final context (commonly <c>ctx =&gt; ctx.Output</c>).</param>
    public Tale<TOutput> Finale(Func<TContext, TOutput> conclusion)
        => new(_steps, ctx => conclusion((TContext)ctx));
}

/// <summary>
/// A sealed tale plan: the ordered steps plus the concluding projection. Produced by
/// <see cref="Tale{TContext,TOutput}.Finale"/> and returned from <c>Tell()</c>. The engine reads
/// <see cref="Steps"/> and applies <see cref="Conclusion"/> when the tale is won.
/// </summary>
/// <typeparam name="TOutput">The output produced when the tale concludes.</typeparam>
public sealed class Tale<TOutput>
    where TOutput : class
{
    internal IReadOnlyList<TaleStep> Steps { get; }
    internal Func<object, TOutput> Conclusion { get; }

    internal Tale(IReadOnlyList<TaleStep> steps, Func<object, TOutput> conclusion)
    {
        Steps = steps;
        Conclusion = conclusion;
    }
}


