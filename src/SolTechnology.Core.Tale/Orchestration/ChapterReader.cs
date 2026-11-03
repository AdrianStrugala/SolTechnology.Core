using Microsoft.Extensions.Logging;
using SolTechnology.Core.Tale.Models;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Reads one chapter of a run and records its outcome (completed, failed, paused or cancelled) in the
/// run state and the chapter's history entry. Only the run's own cancellation escapes as an exception.
/// </summary>
internal sealed class ChapterReader<TInput, TContext, TOutput>(
    ILogger logger,
    TaleRunState state,
    InteractiveChapterInvoker<TInput, TContext, TOutput> interactive)
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
            }
            else if (result.IsSuccess && chapterInfo.Status != TaleStatus.WaitingForInput)
            {
                chapterInfo.Status = TaleStatus.Completed;
                chapterInfo.FinishedAt = state.Now;
            }
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            state.IsCancelled = true;
            chapterInfo.Status = TaleStatus.Cancelled;
            chapterInfo.FinishedAt = state.Now;
            throw;
        }
        catch (OperationCanceledException) when (state.SaveError != null)
        {
            logger.LogInformation("Chapter [{ChapterId}] stopped because the tale is owned by another run", chapter.ChapterId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chapter [{ChapterId}] failed", chapter.ChapterId);
            state.Fail(chapterInfo, UnexpectedFailure.Of($"Chapter [{chapter.ChapterId}] failed unexpectedly."));
        }
    }
}
