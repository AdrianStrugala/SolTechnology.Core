# Story framework — one `StoryHandler.Handle()` call

The runtime view that complements the
[component diagram](./story-framework-components.md): a single
`StoryHandler.Handle(input, ct)` invocation — build the plan, interpret the steps, let a
`Chapter` mutate the shared `Context`, then project the `Output`. The `Caller` actor is whatever
CQRS command/query dispatch invokes the handler. Derived from
[`StoryHandler.cs`](../../src/SolTechnology.Core.Story/StoryHandler.cs) (`Handle`),
[`Tale/Tale.cs`](../../src/SolTechnology.Core.Story/Tale/Tale.cs) (`Finale`),
[`Orchestration/StoryEngine.cs`](../../src/SolTechnology.Core.Story/Orchestration/StoryEngine.cs)
(`Initialize` / `Run` / `ExecuteChapter` / `GetResult`),
[`Chapter.cs`](../../src/SolTechnology.Core.Story/Chapter.cs) (`Read`) and
[`Persistence/IStoryRepository.cs`](../../src/SolTechnology.Core.Story/Persistence/IStoryRepository.cs).

## Sequence diagram

```mermaid
sequenceDiagram
    autonumber
    actor Caller
    box Logic
        participant Handler as StoryHandler
        participant Builder as Tale (builder + plan)
        participant Engine as StoryEngine
        participant Chapter as Chapter
    end
    box Data
        participant Repo as IStoryRepository
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
    opt resuming a persisted story
        Engine->>Repo: FindById(storyId)
        Repo-->>Engine: StoryInstance?
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
        Engine-->>Handler: Result&lt;TOutput&gt;.Fail(Error / StoryPausedError / StoryCancelledError)
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
  to the engine's `Apply*` handlers (step 15). The first failure flips the story to the lost
  track and later steps short-circuit until an `Otherwise` recovers.
- `GetResult` (step 18) projects the final `Context` into `TOutput` on the won track
  (steps 19–21), or surfaces the first `Error` / `StoryPausedError` / `StoryCancelledError`
  otherwise (step 22).


