namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// One recorded step in a <see cref="Tale{TOutput}"/>. A Tale is a plan — a list of these
/// steps — that the <c>TaleEngine</c> later interprets against the live <c>Context</c>. Steps close
/// over the context as <see cref="Tale{TContext,TOutput}"/>; the engine knows the concrete context type and casts.
/// </summary>
internal abstract record TaleStep;

/// <summary>Run a named chapter resolved from DI. Skipped once the Tale has failed (first-error short-circuit).</summary>
internal sealed record ReadStep(Type ChapterType) : TaleStep;

/// <summary>Guard: stay successful only while <see cref="Predicate"/> holds, else fail with <see cref="Error"/>.</summary>
internal sealed record GuardStep(Func<object, bool> Predicate, Error Error) : TaleStep;

/// <summary>Recover from a failure by running a fallback chapter; clears the error and resumes the success track.</summary>
internal sealed record FallbackChapterStep(Type ChapterType) : TaleStep;

/// <summary>Recover from a failure by running an inline fallback; clears the error and resumes the success track.</summary>
internal sealed record FallbackStep(Func<object, Task<Result>> Recover) : TaleStep;

/// <summary>Side-effect that runs only when the Tale has failed. Does not change the track.</summary>
internal sealed record OnLostStep(Func<Error, Task> Effect) : TaleStep;

/// <summary>Side-effect that runs only while the Tale is still succeeding. Does not change the track.</summary>
internal sealed record OnWonStep(Func<object, Task> Effect) : TaleStep;

/// <summary>An inline step that mutates the context and may itself fail.</summary>
internal sealed record InlineStep(Func<object, Task<Result>> Action) : TaleStep;

