namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Interprets a <see cref="Tale{TOutput}"/> plan: walks each recorded step in order, applying the
/// first-error short-circuit. Chapters after a failure are skipped; an <c>Otherwise</c> step can
/// clear the failure and resume.
/// </summary>
internal sealed class TaleStepInterpreter(TaleRunState state, Func<Type, int, Task> executeChapter)
{
    public async Task Run(IReadOnlyList<TaleStep> steps, object context)
    {
        for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            switch (steps[stepIndex])
            {
                case ReadStep s:
                    await executeChapter(s.ChapterType, stepIndex);
                    break;
                case GuardStep s:
                    ApplyGuard(s, context);
                    break;
                case FallbackChapterStep s:
                    await ApplyFallbackChapter(s.ChapterType, stepIndex);
                    break;
                case FallbackStep s:
                    await ApplyFallback(s.Recover, context);
                    break;
                case OnLostStep s:
                    await ApplyOnLost(s.Effect);
                    break;
                case OnWonStep s:
                    await ApplyOnWon(s.Effect, context);
                    break;
                case InlineStep s:
                    await ApplyInline(s.Action, context);
                    break;
            }
        }
    }

    private void ApplyGuard(GuardStep step, object context)
    {
        if (state.OnWonTrack && !step.Predicate(context))
        {
            state.Fail(null, step.Error);
        }
    }

    private async Task ApplyFallbackChapter(Type chapterType, int stepIndex)
    {
        if (!state.OnLostTrack)
        {
            return;
        }

        state.ClearFailure();
        await executeChapter(chapterType, stepIndex);
    }

    private async Task ApplyFallback(Func<object, Task<Result>> recover, object context)
    {
        if (!state.OnLostTrack)
        {
            return;
        }

        state.ClearFailure();
        var result = await recover(context);
        if (result.IsFailure)
        {
            state.Fail(null, result.Error!);
        }
    }

    private async Task ApplyOnLost(Func<Error, Task> effect)
    {
        if (state.OnLostTrack)
        {
            await effect(state.Errors[^1]);
        }
    }

    private async Task ApplyOnWon(Func<object, Task> effect, object context)
    {
        if (state.OnWonTrack)
        {
            await effect(context);
        }
    }

    private async Task ApplyInline(Func<object, Task<Result>> action, object context)
    {
        if (!state.OnWonTrack)
        {
            return;
        }

        var result = await action(context);
        if (result.IsFailure)
        {
            state.Fail(null, result.Error!);
        }
    }
}
