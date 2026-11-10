using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging.Chronicles;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Reads one chapter of a run and records its outcome (completed, failed, paused or cancelled) in the
/// run state, the chapter's history entry and its Chronicle step. Only the run's own cancellation escapes
/// as an exception.
/// </summary>
internal sealed class ChapterReader<TInput, TContext, TOutput>(
    ILogger logger,
    TaleRunState state,
    InteractiveChapterInvoker<TInput, TContext, TOutput> interactive,
    IChronicle? chronicle = null)
    where TInput : class
    where TContext : Context<TInput, TOutput>, new()
    where TOutput : class, new()
{
    public async Task Read(
        IChapter<TContext> chapter,
        ChapterInfo chapterInfo,
        TContext context,
        CancellationToken chapterToken,
        CancellationToken runToken)
    {
        using var step = chronicle?.Step("Chapter " + chapter.ChapterId) ?? default;
        try
        {
            chapterToken.ThrowIfCancellationRequested();
            var result = InteractiveChapterInvoker<TInput, TContext, TOutput>.TryGetInputType(chapter.GetType(), out var inputType)
                ? await interactive.Read(chapter, inputType!, chapterInfo, context, chapterToken)
                : await chapter.Read(context, chapterToken);

            // Rejected interactive input keeps the tale paused for a retry; it is not a failure.
            if (result.IsFailure && !state.IsPaused)
            {
                state.Fail(chapterInfo, result.Error!);
                step.Fail(result.Error!.GetType().Name, result.Error.Message);
            }
            else if (result.IsSuccess && chapterInfo.Status != TaleStatus.WaitingForInput)
            {
                chapterInfo.Status = TaleStatus.Completed;
                chapterInfo.FinishedAt = state.Now;
            }

            if (chapterInfo.Status == TaleStatus.WaitingForInput)
            {
                step.Set("Outcome", "WaitingForInput");
            }
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            step.Cancel();
            state.IsCancelled = true;
            chapterInfo.Status = TaleStatus.Cancelled;
            chapterInfo.FinishedAt = state.Now;
            throw;
        }
        catch (OperationCanceledException) when (state.SaveError != null)
        {
            step.Set("Outcome", "LeaseLost").Cancel();
            logger.LogInformation("Chapter [{ChapterId}] stopped because the tale is owned by another run", chapter.ChapterId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chapter [{ChapterId}] failed", chapter.ChapterId);
            step.Fail(ex);
            state.Fail(chapterInfo, UnexpectedFailure.Of($"Chapter [{chapter.ChapterId}] failed unexpectedly."));
        }
    }
}
