# Tale framework — one `TaleHandler.Handle()` call

The runtime view that complements the
[component diagram](./tale-framework-components.md): a single
`TaleHandler.Handle(input, ct)` invocation — build the plan, interpret the steps, let a
`Chapter` mutate the shared `Context`, then project the `Output`. The `Caller` actor is whatever
CQRS command/query dispatch invokes the handler. Derived from
[`TaleHandler.cs`](../../src/SolTechnology.Core.Tale/TaleHandler.cs) (`Handle`),
[`Tale.cs`](../../src/SolTechnology.Core.Tale/Tale.cs) (`Finale`),
[`Orchestration/TaleEngine.cs`](../../src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs)
(`Initialize` / `Run` / `ExecuteChapter` / `GetResult`),
[`Chapter.cs`](../../src/SolTechnology.Core.Tale/Chapter.cs) (`Read`) and
[`Persistence/ITaleRepository.cs`](../../src/SolTechnology.Core.Tale/Persistence/ITaleRepository.cs).

## Sequence diagram

```mermaid
sequenceDiagram
    autonumber
    actor Caller
    box Logic
        participant Handler as TaleHandler
        participant Builder as Tale (builder + plan)
        participant Engine as TaleEngine
        participant Chapter as Chapter
    end
    box Data
        participant Repo as ITaleRepository
    end
    box Domain
        participant Context as Context
    end
    box External
        participant DI as IServiceProvider
    end

    Caller->>Handler: Handle(input, ct)
    activate Handler

    Handler->>Engine: construct + Initialize(Context, GetType(), resumeInput, ct)
    activate Engine
    opt resuming a persisted tale
        Engine->>Repo: FindById(storyId)
        Repo-->>Engine: TaleInstance?
    end
    Engine-->>Handler: Result.Success()
    deactivate Engine

    Note over Handler,Builder: Tell() builds the plan — it does not execute
    Handler->>Builder: Open&lt;T&gt;() · Read&lt;T&gt;() · Expect · Otherwise · Finale(ctx =&gt; ctx.Output)
    activate Builder
    Builder-->>Handler: Tale&lt;TOutput&gt; (Steps + Conclusion)
    deactivate Builder

    Handler->>Engine: Run(tale.Steps)
    activate Engine
    loop each TaleStep — won / lost track
        alt ReadStep and still on won track
            Engine->>DI: GetService(chapterType)
            DI-->>Engine: IChapter&lt;TContext&gt;
            Engine->>Chapter: Read(Context)
            activate Chapter
            Chapter->>Context: set intermediate / Output fields
            Chapter-->>Engine: Result
            deactivate Chapter
            opt Result.IsFailure
                Engine->>Engine: HandleChapterFailure → switch to lost track
            end
        else Guard / Fallback / OnLost / OnWon / Inline
            Engine->>Engine: ApplyGuard / ApplyFallback / ApplyOnLost / ApplyOnWon / ApplyInline
        end
        opt repository configured
            Engine->>Repo: SaveAsync(intermediate state)
        end
    end
    Engine-->>Handler: steps walked
    deactivate Engine

    Handler->>Engine: GetResult(tale.Conclusion)
    activate Engine
    alt won
        Engine->>Context: conclusion(Context) → read Output
        Context-->>Engine: TOutput
        Engine-->>Handler: Result&lt;TOutput&gt;.Success(output)
    else lost / paused / cancelled
        Engine-->>Handler: Result&lt;TOutput&gt;.Fail(Error / TalePausedError / TaleCancelledError)
    end
    deactivate Engine

    opt repository configured
        Handler->>Engine: PersistTerminalState()
        activate Engine
        Engine->>Repo: SaveAsync(terminal state)
        deactivate Engine
    end

    Handler-->>Caller: Result&lt;TOutput&gt;
    deactivate Handler
```

## Reading the diagram

- Steps 6–7 are the **plan-building** phase: `Tell()` drives the `Tale` builder, each fluent call
  appends a `TaleStep`, and `Finale` seals the immutable `Tale<TOutput>` (its `Steps` and
  `Conclusion`). Nothing has executed yet.
- `Run` (step 8) hands the plan to the **interpreter**; the loop body (steps 9–16) handles one
  `TaleStep` per pass — a `ReadStep` resolves the `Chapter` from DI (steps 9–10) and calls
  `Read(Context)` (step 11), which mutates the shared `Context` (step 12). Other step kinds map
  to the engine's `Apply*` handlers (step 15). The first failure flips the tale to the lost
  track and later steps short-circuit until an `Otherwise` recovers.
- `GetResult` (step 18) projects the final `Context` into `TOutput` on the won track
  (steps 19–21), or surfaces the first `Error` / `TalePausedError` / `TaleCancelledError`
  otherwise (step 22).


