using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Errors;
using SolTechnology.Core.Tale.Models;
using SolTechnology.Core.Tale.Persistence;

namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>
/// Runs an <see cref="InteractiveChapter{TContext,TChapterInput}"/>: pauses the tale when no input
/// was supplied, otherwise deserializes the input and calls <c>ReadWithInput</c>. Rejected input
/// keeps the tale paused for a retry. Method handles are cached per chapter type.
/// </summary>
internal sealed class InteractiveChapterInvoker<TInput, TContext, TOutput>(
    ILogger logger,
    TaleRunState state,
    string handlerTypeName,
    bool isPersisted)
    where TInput : class
    where TContext : Context<TInput, TOutput>, new()
    where TOutput : class, new()
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> SchemaMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> ReadWithInputMethods = new();

    public static bool TryGetInputType(Type chapterType, out Type? inputType)
    {
        for (var type = chapterType; type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InteractiveChapter<,>))
            {
                inputType = type.GetGenericArguments()[1];
                return true;
            }
        }

        inputType = null;
        return false;
    }

    public async Task<Result> Read(
        IChapter<TContext> chapter,
        Type inputType,
        ChapterInfo chapterInfo,
        TContext context,
        CancellationToken cancellationToken)
    {
        var schema = GetSchema(chapter);
        if (schema != null)
        {
            chapterInfo.RequiredData = schema.ToList();
        }

        if (state.ResumeInput == null)
        {
            return Pause(chapter, chapterInfo, context);
        }

        object? input;
        try
        {
            input = JsonSerializer.Deserialize(state.ResumeInput.Value, inputType, TaleJsonOptions.Default);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Input for chapter {ChapterId} could not be deserialized", chapter.ChapterId);
            return RejectInput(chapter, chapterInfo, $"Input for chapter '{chapter.ChapterId}' is invalid at '{ex.Path}'.");
        }

        if (input == null)
        {
            return RejectInput(chapter, chapterInfo, $"Input for chapter '{chapter.ChapterId}' is required.");
        }

        chapterInfo.ProvidedData = state.ResumeInput;
        var result = await InvokeReadWithInput(chapter, context, input, cancellationToken);
        if (result.IsFailure)
        {
            PauseForRetry(chapter, chapterInfo, result.Error);
            return result;
        }

        state.ResumeInput = null;
        chapterInfo.Status = TaleStatus.Running;
        context.CurrentChapterId = null;
        return result;
    }

    private Result Pause(IChapter<TContext> chapter, ChapterInfo chapterInfo, TContext context)
    {
        if (!isPersisted)
        {
            logger.LogWarning(
                "Tale [{TaleHandler}] paused outside TaleManager; the run is not persisted and cannot be resumed.",
                handlerTypeName);
        }

        state.IsPaused = true;
        chapterInfo.Status = TaleStatus.WaitingForInput;
        context.CurrentChapterId = chapter.ChapterId;
        return Result.Success();
    }

    private Result RejectInput(IChapter<TContext> chapter, ChapterInfo chapterInfo, string message)
    {
        var error = new ValidationError { Message = message };
        PauseForRetry(chapter, chapterInfo, error);
        return Result.Fail(error);
    }

    private void PauseForRetry(IChapter<TContext> chapter, ChapterInfo chapterInfo, Error? error)
    {
        logger.LogInformation("Interactive chapter {ChapterId} rejected input: {Error}",
            chapter.ChapterId, error?.Message);
        state.ResumeInput = null;
        state.IsPaused = true;
        chapterInfo.Status = TaleStatus.WaitingForInput;
        chapterInfo.Error = error;
    }

    private static IReadOnlyList<DataField>? GetSchema(IChapter<TContext> chapter)
    {
        var method = SchemaMethods.GetOrAdd(chapter.GetType(), t =>
            t.GetMethod("GetRequiredInputSchema", BindingFlags.Public | BindingFlags.Instance));

        return method?.Invoke(chapter, null) switch
        {
            IReadOnlyList<DataField> fields => fields,
            IEnumerable<DataField> fields => fields.ToList(),
            _ => null
        };
    }

    private static async Task<Result> InvokeReadWithInput(
        IChapter<TContext> chapter, TContext context, object input, CancellationToken cancellationToken)
    {
        var method = ReadWithInputMethods.GetOrAdd(chapter.GetType(), t =>
            t.GetMethod("ReadWithInput", BindingFlags.Public | BindingFlags.Instance));
        if (method == null)
        {
            return Result.Fail($"ReadWithInput not found on {chapter.GetType().Name}");
        }

        return await (Task<Result>)method.Invoke(chapter, [context, input, cancellationToken])!;
    }
}
