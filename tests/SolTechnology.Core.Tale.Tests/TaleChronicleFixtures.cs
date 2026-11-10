using Microsoft.Extensions.Logging;
using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Tale.Tests;

public class ChronicleFirstChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken) => Result.SuccessAsTask();
}

public class ChronicleSecondChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken) => Result.SuccessAsTask();
}

public class ChronicleThirdChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken) => Result.SuccessAsTask();
}

public class ChronicleFailingChapter : Chapter<LifecycleContext>
{
    public override Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken) =>
        ctx.Input.Value == 0
            ? throw new InvalidOperationException("boom")
            : Task.FromResult(Result.Fail(new NotFoundError { Message = "City [Atlantis] not found" }));
}

/// <summary>The caller's cancellation source, shared with <see cref="ChronicleCancellingChapter"/>.</summary>
public sealed class CallerCancellation : IDisposable
{
    public CancellationTokenSource Source { get; } = new();

    public void Dispose() => Source.Dispose();
}

/// <summary>Cancels the caller's token from inside the chapter, as a client disconnect would.</summary>
public class ChronicleCancellingChapter(CallerCancellation caller) : Chapter<LifecycleContext>
{
    public override async Task<Result> Read(LifecycleContext ctx, CancellationToken cancellationToken)
    {
        await caller.Source.CancelAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return Result.Success();
    }
}

public class ChronicleThreeChapterTale(IServiceProvider sp, ILogger<ChronicleThreeChapterTale> log)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, log)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<ChronicleFirstChapter>()
            .Read<ChronicleSecondChapter>()
            .Read<ChronicleThirdChapter>()
            .Finale(ctx => ctx.Output);
}

public class ChronicleFailingTale(IServiceProvider sp, ILogger<ChronicleFailingTale> log)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, log)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<ChronicleFirstChapter>()
            .Read<ChronicleFailingChapter>()
            .Read<ChronicleThirdChapter>()
            .Finale(ctx => ctx.Output);
}

public class ChronicleCancelledTale(IServiceProvider sp, ILogger<ChronicleCancelledTale> log)
    : TaleHandler<LifecycleInput, LifecycleContext, LifecycleOutput>(sp, log)
{
    protected override Tale<LifecycleOutput> Tell() =>
        Open<ChronicleFirstChapter>()
            .Read<ChronicleCancellingChapter>()
            .Read<ChronicleThirdChapter>()
            .Finale(ctx => ctx.Output);
}
