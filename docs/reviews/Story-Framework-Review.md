# Code Review — `SolTechnology.Core.Story`

**Reviewer:** Senior .NET engineer / architect  
**Zakres:** implementacja (`src/SolTechnology.Core.Story`), testy (`tests/SolTechnology.Core.Story.Tests`), dokumentacja (`docs/Story.md`, `docs/adr/002-Story-Framework-Implementation.md`, `src/SolTechnology.Core.Story/README.md`, `CLAUDE.md`), oraz rzeczywiste użycie (`sample-tale-code-apps/DreamTravel`).  
**Werdykt jednym zdaniem:** Framework ma dobry, czytelny pomysł (Tale Code, chaptery, persistence), ale w obecnym kształcie zawiera **istotne błędy korektności, mocny dryf dokumentacji od kodu, nadużycie refleksji/`dynamic`, kilka footgunów DI** oraz **zdecydowanie przesadzony status „Production Ready”** w ADR.

---

## 1. Spójność nazewnictwa i dokumentacji ↔ kod (🔴 krytyczne)

Framework ma **dwa różne „języki”** — jeden w kodzie, drugi w dokumentacji:

| Dokumentacja mówi | Kod faktycznie ma |
|---|---|
| `Narration<TInput,TOutput>` (ADR, README core, Story.md) | `Context<TInput,TOutput>` (plik `Context.cs`) |
| `services.AddStoryFramework(...)` (`docs/Story.md`, `CLAUDE.md`) | `services.RegisterStories(...)` |
| `StoryManager.GetStoryState(string storyId)` (README API Reference) | `GetStoryState(Auid storyId)` |
| `StoryManager.ResumeStory<>(string storyId, ...)` (README) | `ResumeStory<>(Auid storyId, ...)` |
| `IStoryRepository` ma `FindById(string)` (README – CosmosDbStoryRepository example) | `FindById(Auid)` |
| `public bool EnablePersistence { get; set; }` (README API) | `{ get; init; }` |
| `DataField { Required, Description }` (README – „Best practice: Interactive chapter”) | `DataField { Name, Type, IsComplex, Children }` — pola `Required`/`Description` **nie istnieją**. Przykład z dokumentacji **nie skompiluje się.** |
| Przykład użycia handlera: `serviceProvider.GetRequiredService<SaveCityStory>()` (README) | `ModuleInstaller.RegisterStories` rejestruje wyłącznie `IChapter<>`. `StoryHandler` NIE jest rejestrowany. Kod z README rzuci `InvalidOperationException` at runtime. |
| „Simple automated story” używa `Handle(input)` bez `StoryManager` | W README „Troubleshooting: Story doesn't pause at interactive chapter” mówi się: „❌ Wrong – direct execution … ✅ Use StoryManager”. Oba rozdziały są w **tej samej dokumentacji**, ale **sprzeczne**. |
| Status ADR: `ADR-001` | Nazwa pliku: `002-Story-Framework-Implementation.md` |

W `docs/Story.md` nagłówek mówi `public class ProcessOrderStory : StoryHandler<OrderInput, Ordercontext, OrderOutput>` — literówka `Ordercontext` (małe `c`). Powtarza się wielokrotnie.

**Wpływ:** nowy użytkownik nie ma szansy, by `copy-paste` działał. Dryf jest tak duży, że dokumentacja sprawia wrażenie napisanej **przed** finalnym zaimplementowaniem API (prawdopodobnie tak było — `Narration` → `Context` to refactor, który nie został przepropagowany do dokumentacji).

**Rekomendacja:** jednorazowo zdecydować `Narration` vs `Context` (preferuję `Narration`, bo `Context` zbyt koliduje z `HttpContext`, `DbContext`, `OperationContext` w IntelliSense), następnie wykonać mechaniczny refactor + test dokumentacji (minimalne uruchomienie wszystkich przykładów z README w osobnym projekcie `Story.Samples`).

---

## 2. Błędy korektności (🔴 krytyczne)

### 2.1. Bezpośrednie użycie `Handle()` + persistence = story nigdy nie osiąga `Completed`

`StoryEngine.SaveStoryState` ustawia status jako:

```
_isPaused ? WaitingForInput : _hasFailed ? Failed : Running
```

Nigdy `Completed`. Finalizację do `Completed` robi **tylko** `StoryManager.MarkAsCompleted`. Czyli:

- Ścieżka reklamowana w README („Direct Handler Usage for simple workflows”) z włączonym persistence **zostawia rekord w DB w stanie `Running` na zawsze**.
- Dla `SqliteStoryRepository` to leak tabeli `StoryInstances` rosnący liniowo, plus false-positive dla zapytań typu „aktywne workflow”.

### 2.2. `SaveStoryState` nadpisuje `CreatedAt` za każdym razem

```csharp
var instance = new StoryInstance
{
    ...
    CreatedAt = DateTime.UtcNow, // ← bug: powinno być ładowane z repo
    LastUpdatedAt = DateTime.UtcNow,
    ...
};
await repository.SaveAsync(instance);
```

Każdy `SaveAsync` w SQLite robi `ON CONFLICT DO UPDATE` **ale nie aktualizuje** `CreatedAt` (dobrze), natomiast InMemory repo nadpisuje cały obiekt — tracimy oryginalny czas startu. Audyt historyczny („kiedy ta paused story ruszyła pierwszy raz”) jest nieuzyskiwalny w InMemory, a w SQLite — działa tylko przypadkiem.

### 2.3. Pause wykrywany przez `Contains("paused")` — string coupling

`StoryEngine.GetResult`:

```csharp
if (_isPaused)
    return Result<TOutput>.Fail(new Error { Message = "Story paused waiting for user input" });
```

`StoryManager.ExecuteStoryPipeline`:

```csharp
if (result.IsFailure && result.Error?.Message.Contains("paused") == true)
    return await GetLatestInstance(activeStoryId);
```

`StoryController` (dwukrotnie):

```csharp
if (errorMessage?.Contains("paused") == true) { ... return Ok(...); }
```

- Magic string „paused” w trzech miejscach. Każdy, kto zlokalizuje komunikat albo zmieni treść (np. dla telemetrii) — zepsuje wszystko.
- Semantyka jest błędna: **pauza to nie błąd**. Wszystkie testy asertujące `result.IsSuccess.Should().BeTrue()` po `StartStory` polegają na tym, że `StoryManager` **konwertuje** `Fail` na `Success`. Czyli:
  - `StoryHandler.Handle()` zwraca `IsFailure = true` przy pauzie.
  - `StoryManager.StartStory` zwraca `IsSuccess = true` i te same dane.
  - Pojęcie „sukcesu” jest zdefiniowane inaczej na różnych poziomach stosu.

**Fix:** wprowadzić wartość stanu w `Result`/engine (`StoryExecutionStatus.Paused`) albo osobny typ, nie klejone tekstem.

### 2.4. `ModuleInstaller` używa `Assembly.GetCallingAssembly()` — znany footgun

```csharp
var callingAssembly = Assembly.GetCallingAssembly();
```

Rzeczywiste użycie w DreamTravel:

```csharp
// sample-tale-code-apps/DreamTravel/src/LogicLayer/DreamTravel.Workflows/ModuleInstaller.cs
public static IServiceCollection AddFlows(this IServiceCollection services, StoryOptions? options = null)
    => services.RegisterStories(options);
```

Gdy użytkownik opakuje `RegisterStories` w swoją extension method (co jest idiomatyczne w DreamTravel), `GetCallingAssembly()` zwraca **assembly wrappera**, nie aplikację hosta — więc auto-discovery **nie znajdzie** rozdziałów z innych warstw. Każdy moduł wymaga manualnej rejestracji (zaprzecza obietnicy auto-discovery).

**Fix:** wymagać od użytkownika `params Assembly[] assemblies` albo zestawu `Type[] markerTypes`, tak jak robi MediatR 12+. Domyślnie brać `Assembly.GetEntryAssembly()` lub **wszystkie** assemblies z `AppDomain.CurrentDomain` z filtrem. Dodatkowo udokumentować zachowanie.

### 2.5. `StoryHandler` nie jest rejestrowany w DI

`RegisterStories` skanuje tylko `IChapter<>`. Nie rejestruje implementacji `StoryHandler<,,>`. Tymczasem:

- README, Story.md i ADR pokazują `serviceProvider.GetRequiredService<SaveCityStory>()` / wstrzykiwanie przez konstruktor (`OrderController`).
- Żeby to zadziałało, handler musiałby być `services.AddTransient<SaveCityStory>()` manualnie.
- `StoryManager` obchodzi to używając `ActivatorUtilities.CreateInstance<THandler>(serviceProvider)` — ale to działa tylko przez `StoryManager`.

**Fix:** albo dodać rejestrację klas dziedziczących po `StoryHandler<,,>` do skanu, albo explicite udokumentować, że „handler sam się nie rejestruje — wstrzykiwany tylko przez `StoryManager`”. Spójność z dokumentacją jest zerowa.

### 2.6. Detekcja interactive chaptera przez `BaseType`

`StoryEngine.ExecuteChapter`:

```csharp
var isInteractive = typeof(TChapter).BaseType?.IsGenericType == true &&
    typeof(TChapter).BaseType?.GetGenericTypeDefinition() == typeof(InteractiveChapter<,>);
```

To **łamie się**, gdy ktoś zrobi hierarchię (`class MyBaseApprovalChapter : InteractiveChapter<...>`, potem `class ConcreteApproval : MyBaseApprovalChapter`). `BaseType` to wtedy `MyBaseApprovalChapter`, nie `InteractiveChapter<,>`. Engine wywoła `chapter.Read()` (jawna implementacja interfejsu), która **rzuca `InvalidOperationException`** — co jest drogą przez rollback.

**Fix:** iterować po `BaseType` w górę albo sprawdzać `IsAssignableTo(typeof(InteractiveChapter<,>))` z generic-type walker.

### 2.7. `dynamic` + `RuntimeBinderException` tam, gdzie nie ma potrzeby

`StoryEngine<TContext>` ma constraint `TContext : class`, ale używa `dynamic` do odczytu pól, które **są znane w czasie kompilacji** (`StoryInstanceId`, `Output`, `CurrentChapterId`) bo faktyczny typ musi dziedziczyć po `Context<,>` (wymagane przez `StoryHandler` constraint). Co więcej — rezygnuje z constraintu w engine i przez to:

- musi łapać `RuntimeBinderException`,
- płaci koszt dyn-call,
- traci type-safety,
- ma polskie komentarze typu `// Jeśli właściwość nie istnieje, traktujemy jako Empty` w angielskim kodzie.

**Fix:** dodać drugi parametr generyczny `StoryEngine<TInput, TContext, TOutput> where TContext : Context<TInput, TOutput>`. Zniknie cały `dynamic` i wszystkie `try/catch(RuntimeBinderException)` (są 3).

### 2.8. `StoryInstance` — dwóch konstruktorów, `System.Text.Json` może wybrać zły

```csharp
public StoryInstance(Auid storyId, string handlerTypeName, string context) { ... }
public StoryInstance() { }
```

Bez `[JsonConstructor]` STJ może wybrać bezparametrowy, ale zachowanie różni się między wersjami i między deserializacją przy starcie/zresumowanu. `InMemoryStoryRepository.Clone()` robi pełny round-trip JSON — jeśli STJ wybierze ctor z `required`'ami, straci `Status`, `History`, `CurrentChapter`, `Context` (tylko jeden z nich poleci do ctora). Bug na styku InMemory repo + ewolucja typu.

**Fix:** `[JsonConstructor]` na parameterless + init-only, albo usunąć wieloparametrowy konstruktor.

### 2.9. `ChapterInfo.Error` vs `AggregateError` — polimorfizm w JSON

`ChapterInfo.Error : Error?`, gdzie `Error` to klasa z `CQRS.Errors`. Przy deserializacji z SQLite lub JSON-cloning w InMemory **traci** runtime-type (np. `AggregateError`) — chyba że `JsonSerializerOptions` ma `TypeInfoResolver` z polymorphizmem albo `[JsonDerivedType]`. W kodzie — brak.

**Fix:** jeżeli kiedykolwiek chapter ma zwrócić `AggregateError`, albo ustawić polimorfizm STJ, albo spłaszczyć do DTO.

### 2.10. SqliteStoryRepository — brak retry na `SQLITE_BUSY`

Connection-per-operation jest OK, ale:

- Brak pooling (otwieranie/zamykanie fizycznego połączenia na każdą operację). Latencja nie 50 ms (jak mówi ADR) tylko potrafi być gorsza przy lekkich commitach.
- Przy współbieżnym `SaveAsync` z innego wątku możliwe `SqliteException SQLITE_BUSY` — kod nie ma retry policy.
- Brak transakcji — jak opisuje ADR „ACID guarantees” — ale jeden `INSERT ON CONFLICT UPDATE` jest atomic tylko per rekord; nie ma tranzakcyjności między SaveAsync + jakąkolwiek akcją zewnętrzną chaptera.
- Brak opcji connection string do opróżnienia cache lub WAL mode (`journal_mode=WAL`) — dla prawdziwej współbieżności pisarz/czytacz WAL jest de facto wymagany.

### 2.11. `StoryController` — `AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypes())`

```csharp
var handlerType = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .FirstOrDefault(t => t.Name == handlerTypeName && !t.IsAbstract);
```

Problemy:

- `GetTypes()` **rzuca** `ReflectionTypeLoadException` na assembly z trudnymi zależnościami (np. `Microsoft.CodeAnalysis.*`, analyzery, dynamiczne pluginy). Brak `try/catch` z `LoaderExceptions`.
- `FirstOrDefault(t.Name == ...)` — **kolizja krótkich nazw**. Jeśli ktoś zdefiniuje `OrderStory` w dwóch różnych namespace'ach, controller weźmie pierwszy znaleziony. Gorzej: dwa handlery o tej samej nazwie, ale różnym zachowaniu — niedeterministycznie.
- **Brak autoryzacji**. `POST /api/story/{handlerTypeName}/start` bez filtrowania whitelisty typów = każdy handler w procesie jest wywoływalny po nazwie. To **lateral movement risk**: jeśli w aplikacji istnieje handler `AdminDeleteEverythingStory`, atakujący go wywoła (po ApiKey być może, ale to i tak za słabe).
- Brak rate-limitingu / idempotency-key dla `StartStory`.
- `GetStoryResult` ma komentarz: „For now, return the full story instance. In a real implementation, you might want to extract just the Output…” — endpoint reklamowany w ADR jako `GET /{storyId}/result` **nie implementuje kontraktu**. To jest TODO z lipca udostępnione jako API.

**Fix:** whitelist handlerów z ModuleInstallera (np. cache `Dictionary<string, Type>`), wymuszona autoryzacja (`[Authorize]`), zwracanie `StoryInstanceDto` + deserializowany `TOutput` w `/result`.

### 2.12. `Auid` jako string-based primary key w SQLite

Brak normalizacji. `Auid.ToString()` kontra `Auid.Parse()`. Jeśli ktoś przemieli DB między wersjami biblioteki AUID, PK się rozjedzie. Minor.

### 2.13. `StoryController` — reflection-cost na każde żądanie

`GetMethod("StartStory").MakeGenericMethod(...)` → `Invoke(...)` **bez cache**. Na każde `POST /start` robimy 4× reflection (z iteracją po wszystkich assemblies do szukania handler-type'u). Dla HTTP endpointu to dużo overhead + allocation pressure. Fix: `ConcurrentDictionary<(string, Type), MethodInfo>`.

### 2.14. `StoryHandler` ma publicznie ustawialne `Context`

```csharp
public TContext Context { get; set; } = null!;
```

- Handlery są rejestrowane jako `Transient` przez samego użytkownika (wg dokumentacji) — więc teoretycznie instancja jest świeża per request.
- Ale jeśli ktoś zarejestruje je `Scoped` lub `Singleton` (a nic w API tego nie zabrania), dostaniemy **wyścig między requestami** współdzielącymi handler, bo `Handle` modyfikuje `this.Context`. Klasycznie – ukryty stan w klasie stateless API.

**Fix:** zrobić `internal set`, lub w ogóle usunąć pole publiczne i trzymać stan wyłącznie w `StoryEngine` tworzonym per-invocation.

### 2.15. Replay deterministyczny NIE jest gwarantowany

`TellStory()` jest wywoływany od początku przy każdym resume. Engine skraca egzekucję dla już wykonanych chapterów (`Status == Completed`). Ale:

- DI-resolve nadal się wykonuje — jeśli chapter ma ctor ze side-effectami (np. otwiera connection), dostajemy powtórzony side-effect.
- Jeśli `TellStory()` zawiera gałęzie `if (narration.X) await ReadChapter<Y>();`, a chaptery wcześniejsze zmieniły `narration.X`, po resume decyzja musi być **taka sama** (bo oparta o stan w narration). Co OK — ale jeśli zależy od **czegoś spoza narration** (data/godzina, `IRandom`, feature flag), dostajemy rozbieżność ścieżki resume vs oryginalnej. Dokumentacja tego nie opisuje i nie testuje.

### 2.16. Brak wersjonowania definicji story

`StoryInstance` nie ma `HandlerVersion` / `SchemaVersion`. Scenariusz:

1. Deployment v1 z chapterami A→B→C. Users uruchamiają story, pauza na B.
2. Deployment v2 z chapterami A→B'→C (B' zastępuje B, inny input schema).
3. Resume zapisanej story: deserializacja `Narration` się udaje (kompatybilna), B' wykonuje się z inputem starej struktury → cicha korupcja.

ADR wspomina to jako „Future Enhancement” ale **narzędzie bez tego nie powinno być nazywane Production Ready dla długoterminowych workflowów**.

### 2.17. Cancellation niedokończone

`StoryStatus.Cancelled` istnieje w enumie, ale:

- `StoryEngine` nigdy nie ustawia tego statusu.
- `StoryManager` nie ma metody `CancelStory(storyId)`.
- `StoryController` nie ma endpointa do anulowania.

Tylko `CancellationToken` z `Handle()` powoduje `catch (OperationCanceledException)` → `Result.Fail("Story execution cancelled")`. Stan w repo pozostaje `Running`/`WaitingForInput`. Leak.

### 2.18. Idempotentność `StartStory` — brak

Retry po stronie klienta (sieć padła) → druga identyczna story, drugi `StoryId`. Brak `correlationId` / `idempotencyKey` w API. Dla endpointów reklamowanych jako REST to minimum.

---

## 3. Dependency Injection / lifetime (🟠 poważne)

### 3.1. `StoryOptions` zarejestrowane jako singleton + `options.Repository` to singleton

```csharp
services.AddSingleton(options);
services.AddSingleton<IStoryRepository>(options.Repository);
```

Repository jest **dwukrotnie** dostępne: bezpośrednio jako `IStoryRepository` i przez `options.Repository`. Modyfikacja po `Build()` → niezdefiniowana.

### 3.2. `StoryManager` — `Scoped`, ale trzyma `IServiceProvider` root-scope

`RegisterStories` dodaje `AddScoped<StoryManager>()`. OK. Ale `StoryManager` dostaje `IServiceProvider` — i w `ExecuteStoryPipeline` woła `ActivatorUtilities.CreateInstance<THandler>(serviceProvider)`. Jeśli `StoryManager` jest rozwiązany z root providera (np. w background service bez scope), to handler dostanie root-scope → captive dependencies (Scoped DbContext uwięziony jako Transient handler).

**Fix:** explicite wymusić `IServiceScopeFactory` + nowy scope na każde `StartStory`/`ResumeStory`. Dodatkowo — dokumentacja nic o tym nie mówi.

### 3.3. Chaptery zawsze `Transient`

Jeśli chapter potrzebuje DbContext (Scoped), a jest zarezolwowany przez root-provider (bo `StoryEngine` używa `serviceProvider` przekazanego do handlera), mamy captive dep. Widzieliśmy to wyżej — sytuacja rozwiązywalna przez scope-per-invocation.

### 3.4. Brak opcji customizacji factory

Brak callbacku typu `ConfigureChapterRegistration(services => ...)`. Jeśli chcę chapter jako Scoped (np. współdzielony stan w ramach jednego `Handle`), nie ma API.

---

## 4. API design — mniejsze smells (🟡 średnie)

- **`Context<TInput,TOutput>.Output = new()`** — `Output` jest zainicjalizowany mutowalnie. Większość kodu robi `context.Output.Status = "Completed"` dopiero na końcu. Taki wzorzec zmusza do `TOutput : class, new()` co wyklucza record z init-only props / DTO immutable.
- **`IChapter<TContext>` default implementation `ChapterId => GetType().Name`** oraz **`Chapter<>`/`InteractiveChapter<>`** ponownie deklarują `virtual string ChapterId`. Redundancja — `Chapter` może po prostu implementować interfejsowy default albo nadpisywać. Obecne rozwiązanie prowadzi do „ukrycia” interfejsowej definicji (C# 8 DIM quirks).
- **`InteractiveChapter.Read()` rzuca wyjątkiem** zamiast błędu kompilacji. To jest sygnał, że rozróżnienie „czy chapter jest interaktywny” lepiej wyrazić przez **dwie osobne hierarchie** niż jedną (`IInteractiveChapter<TContext, TInput>` bez dziedziczenia po `IChapter<TContext>`). Aktualnie `InteractiveChapter` *jest* `IChapter`, ale jedna z jego metod rzuca. Liskov sformułowany wyraźnie.
- **`StoryOptions.Default` vs `StoryOptions.WithInMemoryPersistence()`** — statyczna fabryka tworzy nowy `InMemoryStoryRepository()` per wywołanie. Jeśli user dwukrotnie wywoła `WithInMemoryPersistence()` dla różnych modułów — dostanie dwie izolowane mapy, a DI zarejestruje tylko jedną. Nie-obvious.
- **Brak `IAsyncDisposable`** na `SqliteStoryRepository` — jeśli kiedykolwiek dodacie connection pool / persistent connection, zmiana będzie breaking.
- **Brak `ILoggerFactory` routingu** — engine loguje przez `ILogger` handlera. Powinno logować przez kategorię `SolTechnology.Core.Story.StoryEngine` dla filtrowania w Serilog/AppInsights.
- **`Auid.New("STR")` — hardkodowany prefix** w engine. Powinien być stałą w `StoryOptions` (np. `StoryIdPrefix`) — inaczej wszystkie story mają ten sam prefix, co uniemożliwia wielo-tenant separation.

---

## 5. Persistence — dodatkowe uwagi (🟡 średnie)

- **Brak index na `HandlerTypeName`** w SQLite — realny query „pokaż wszystkie aktywne instancje handlera X” pełny table scan.
- **Brak metody `ListAsync(filter)` / pagination** — w praktyce operator nie może dowiedzieć się, ile pending stories ma w systemie. Dashboardy niemożliwe bez bezpośredniego SQL.
- **Brak TTL / cleanup policy** — completed stories leżą w DB wiecznie.
- **`Clone()` przez STJ round-trip** — robocze, ale:
  - Traci typ runtime dla polimorfizmów (patrz 2.9).
  - W InMemory test-repo używany w CI ma realny koszt allocacji (ADR sam przyznaje 2x). Dla 100 concurrent tests w `AdvancedScenariosTests` to wymierny narzut.
  - **`MemberwiseClone()`** byłby szybszy i zachowywał runtime-type.

---

## 6. Testy (🟡 średnie)

Objętość 3461 linii testów w 9 plikach to przyzwoity poziom; natomiast:

### Pokrycie
- **`StoryController` — 0 testów.** Controller jest w 80 % refleksją sklejaną stringami, i to on jest fasadą zewnętrznego API. Nie mając testów, nie wiadomo, czy:
  - Obsługuje puste body,
  - Zwraca poprawny HTTP code (dziś zwraca `BadRequest` dla pauzy, chociaż ciało ma `Success`),
  - Obsługuje kolizje krótkich nazw handlerów.
- **`SqliteStoryRepository` — 11 testów**, ale **żaden** test współbieżności (mimo że README i ADR reklamują thread-safety SQLite). Klaim „14 thread-safety tests passing” w ADR odnosi się do InMemory, nie SQLite.
- **Brak testów dla `StoryManager.GetStoryState`** z uszkodzonym JSON contextu — jak system reaguje na uszkodzony rekord?
- **Brak testów dla resume z różną wersją `TContext`** (schema evolution).
- **Brak property-based / fuzz testów** dla `ReadWithInput` (co jest logicznym wymaganiem bo deserializuje dowolny JSON użytkownika).
- **Brak performance/load tests** (ADR twierdzi, że są „100 concurrent stories benchmark” — w repo brak).

### Jakość istniejących testów
- **`AdvancedScenariosTests.Resume_WithExtraFields_ShouldIgnoreAndSucceed`** zakłada, że STJ z `JsonSerializerOptions` w frameworku ignoruje extra pola. W engine `StoryJsonOptions.Default` nie ustawia `UnmappedMemberHandling`, więc zachowanie zależy od domyślnego STJ — test **przepuszczalny w zależności od runtime**.
- **`ErrorHandlingTests`** nie sprawdza, co się dzieje, gdy `Chapter` rzuca `OperationCanceledException` z pośredniego `CancellationToken` (nie wstrzykuje `CancellationToken` do chapterów — kolejny gap).
- **`PauseResumeIntegrationTests.Story_ShouldPreserveContext_AcrossPauseResume`** weryfikuje `storyInstance.Context` jako string JSON — brak testu, że po resume context jest deep-equal do oryginału z uwzględnieniem `DateTime.Kind`, decimali z przecinkiem w różnych kulturach itd.
- **Brak testów dla `StopOnFirstError = false` z wyjątkiem (nie-Result-Fail)** — engine w `catch` ustawia `_hasFailed = true` przez `HandleChapterFailure`, ale nie ma testu czy kolejne chaptery się uruchamiają.
- **Testy nie weryfikują logiki idempotencji `ExecuteChapter`** — „already Completed → return”. W `AdvancedScenariosTests` jest coś bliskiego, ale brak explicite.

### Infrastruktura testów
- Mieszany `[TestFixture]` NUnit + `FluentAssertions 7` + `Microsoft.Extensions.Logging 10.0.0` (to jest preview/aktualna wersja?). Żadnego `Directory.Build.props` zharmonizowania wersji ze story-core.
- `AdvancedScenariosTests` — 856 linii w jednym pliku. Powinno być podzielone na `ResumeTests`, `InputValidationTests`, `ConcurrencyTests`, `SecurityTests`.

---

## 7. ADR (`002-Story-Framework-Implementation.md`) — zastrzeżenia (🟠)

ADR jest **mocno marketingowy** i zawiera twierdzenia, których nie da się zweryfikować z kodu/testów:

- **„Test Coverage ~95%”** — w repo nie ma konfiguracji coverage'u (`coverlet`, `dotCover`) ani raportu. Liczba z sufitu.
- **„Performance Overhead ~2%”** — brak jakiegokolwiek benchmarka (BenchmarkDotNet project nie istnieje).
- **„Successfully Migrated 3 DreamTravel use cases … -23%, -18% LOC”** — weryfikacja wymagałaby Git-diffa. Nie wiem, czy prawdziwe; zapach marketingowy.
- **„Zero breaking changes”** — nie-prawda. Zastępując `Flow` nowym modułem, konsumenci `SolTechnology.Core.Flow` mają breaking path. ADR sam dalej wspomina o [Obsolete] attrs które **nie zostały dodane** (flag „⏳ To be added” — ale ADR jest oznaczony ✅ PRODUCTION READY).
- **„Security review ⭐⭐⭐⭐⭐ No critical issues found”** ale dalej są **HIGH PRIORITY 🔴** rekomendacje z ⏳ To be implemented. Rating nie zgodny z treścią.
- **„Stakeholder Sign-Off: Claude Sonnet 4.5”** jako aprobujący — pojawia się w tabeli decyzyjnej. ADR to *dokument projektowy* — AI assistant nie jest stakeholderem. Metodologicznie to mylące.
- **ADR-001 vs plik 002** — mismatch.
- ADR powołuje się na „Story-Implementation-Plan.md” w dokumentacji i README — **tego pliku nie ma** w `/docs` (tylko Story.md i ADR).

ADR powinien być przeredagowany jako czysto techniczny dokument decyzyjny (kontekst → decyzja → konsekwencje → status) bez marketingu.

---

## 8. Bezpieczeństwo (🟠)

- **SqliteStoryRepository przyjmuje dowolny path** (`SqliteStoryRepository(string?)`). Brak walidacji. Jeśli kontroler albo konfiguracja pozwalają użytkownikowi wpłynąć na ten path (np. multi-tenant SaaS pobierający tenantId z headera → łączy z `/data/stories_{tenant}.db`), dostajemy path traversal. ADR sam to flaguje jako HIGH PRIORITY i… zostawia nieuskutecznione.
- **Brak encryption-at-rest** dla SQLite — narracja może zawierać PII/klucze API. README mówi: „Best practice: nie przechowuj sensitive data” ale framework nie daje żadnego mechanizmu ich odrzucenia (np. `[NonPersisted]` attr → sanitize before save).
- **`StoryController` bez autoryzacji** — domyślnie otwarty endpoint POST z arbitralnym JSON body przetwarzanym przez refleksję — patrz 2.11.
- **Brak walidacji wielkości inputu** — `ReadWithInput` dostaje dowolny JSON, deserializuje do `TChapterInput`. Brak limitu (np. 16 MB body → 16 MB PayloadTooLarge).
- **Brak rate-limit** / replay-protection dla `/resume`.

---

## 9. Obserwowalność (🟡)

- Brak **OpenTelemetry** / `ActivitySource`. Framework workflowowy bez distributed tracing w 2026 to kuriozum (Durable Functions, Temporal, Elsa — wszystkie mają).
- Brak **metryk** (licznik aktywnych story, histogram czasu per chapter, counter błędów). ADR o tym wspomina jako future — OK, ale bez tego „production ready” jest na wyrost.
- Logi używają typów placeholders (`{Handler}`, `{ChapterId}`) — spójne. +
- Brak **health check**'a dla SQLite repo.
- Brak **correlationId** propagowanego automatycznie do logu chaptera (dobrą praktyką byłby `BeginScope(storyId)`).

---

## 10. Potencjalne przypadki użycia — ocena dopasowania

Wzorzec orkiestracji jak ten (liniowy pipeline z pauzą) pasuje do:

| Use case | Dopasowanie | Komentarz |
|---|---|---|
| Wizard formularzowy (multi-step onboarding) | ✅ doskonałe | InteractiveChapter idealnie opisuje krok formularza. |
| Workflow zatwierdzania (document approval) | ✅ dobre | Potrzeba jednak explicit cancel — niedomówione (patrz 2.17). |
| Checkout sklepu internetowego | 🟡 częściowe | Brak **kompensacji/saga** — jeśli płatność przejdzie, a magazyn padnie, nie ma rollbacku. ADR wymienia to jako future. |
| Długo-żyjące procesy biznesowe (dni/tygodnie) | 🔴 słabe | Brak wersjonowania (2.16), brak TTL, brak schedulera (resume tylko ręcznie). |
| Parallel fan-out / map-reduce | ❌ nie obsługiwane | `TellStory` jest jawnie sekwencyjne; brak prymitywu „parallel ReadChapter”. |
| Conditional branching w DSL | 🟡 | Działa przez IF w C#, ale traci „deklaratywność” i wymaga idempotentnego replay. |
| Integracje event-driven (Kafka/Service Bus → start story) | 🟡 | Sam framework tego nie pokrywa, ale łatwo dopiąć z zewnątrz. `StoryManager` jest adekwatnym entry-point. |
| Sagi rozproszone | ❌ | Jawnie poza scope v1. |
| Short-lived in-process pipelines bez persistence | ✅ | Najlepszy fit — tu `Chain`→`Story` refactor był uzasadniony. |
| Batch processing (milion rekordów) | ❌ | Narration trzymany w całości w pamięci + serializowany do JSON per chapter → nie do użycia. |
| Retry z backoffem | ❌ | Brak abstrakcji Polly/reliability. Chapter który rzuci wyjątkiem → `Fail`, koniec. |
| Human-in-the-loop + notyfikacje e-mailem | 🟡 | Story może pauzować, ale framework nie emituje zdarzeń „pauza powstała”, nie integruje się z notyfikacjami. Trzeba dopisać. |

**Wniosek:** Framework jest adekwatny dla **prostej orkiestracji kroków z pauzą na input użytkownika w obrębie jednego procesu**. Sprzedawanie go jako generycznego „workflow engine” (jak sugeruje ADR porównaniem z Temporal/Durable) jest nieadekwatne.

---

## 11. Drobiazgi i kosmetyka

- Polskie komentarze w angielskim projekcie (`StoryEngine.cs:75`, `:196`).
- `ModuleInstaller` nazwany tak samo jak wiele innych instalerów — unikalizacja np. `StoryModuleInstaller` ułatwiłaby grep.
- `SqliteStoryRepository.EnsureDatabaseInitialized()` — `_isInitialized` to pole instancji; dla dwóch instancji tego samego DB zrobi CREATE dwukrotnie (idempotentne, ale niepotrzebne). Można zrobić static lock per path.
- `StoryOptions` z `init`-only properties jest niekompatybilne z bind-from-configuration (`IConfiguration.Bind`). Tester chcący wyciągnąć konfig z `appsettings.json` nie ma jak.
- Brak `nullability annotations` w niektórych miejscach (`StoryInstance.HandlerTypeName = default!;`).
- README `Installation` mówi o pakiecie `SolTechnology.Core.Story` na NuGet — sprawdziłbym, czy `.csproj` ma `GeneratePackageOnBuild` i packable metadata. Brak wiedzy bez otworzenia.
- `StoryInstance` ma mieszane `init` i `set` (`HandlerTypeName { init; }` ale `Status { set; }`) — niekonsekwentnie.
- `StoryInstanceDto.StoryId` jako string — traci informację typu. Klienci muszą znać semantykę Auid.

---

## 12. Podsumowanie — priorytety naprawcze

### 🔴 Krytyczne (blocker dla „Production Ready”)
1. Zsynchronizować dokumentację z kodem (`Context` vs `Narration`, `RegisterStories` vs `AddStoryFramework`, DataField fields, typ `storyId`).
2. Naprawić niespójność stanu — `Handle()` bezpośrednie z persistence zostawia `Running` (sekcja 2.1).
3. Usunąć `string.Contains("paused")` na rzecz dedykowanego stanu (sekcja 2.3).
4. Rozwiązać `Assembly.GetCallingAssembly()` footgun w `RegisterStories` (sekcja 2.4).
5. Autoryzacja + whitelist handlerów w `StoryController`; obsługa `ReflectionTypeLoadException` (2.11).
6. Wersjonowanie `StoryInstance` albo jawne oznaczenie „nie dla long-running” (2.16).
7. Zarejestrować handlery w DI lub konsekwentnie udokumentować, że handler tylko przez StoryManager (2.5).

### 🟠 Poważne
8. Usunąć `dynamic` i `RuntimeBinderException` przez poprawny generic constraint (2.7).
9. Dokończyć cancellation i dodać endpoint `CancelStory` (2.17).
10. Dodać schema-evolution checks (rejected resume ze starą wersją albo migration hook).
11. Naprawić wykrywanie `InteractiveChapter` w hierarchii dziedziczenia (2.6).
12. Przepisać ADR na trzeźwo (pozbyć się marketingu, dodać realne pomiary).
13. Ogarnąć DI lifetimes (`IServiceScopeFactory` per-invocation w `StoryManager`).

### 🟡 Średnie
14. Testy dla `StoryController`, SQLite concurrency, schema-evolution, cancellation.
15. OpenTelemetry + metryki + logging scope z `storyId`.
16. Paginated listing + index na `HandlerTypeName`, TTL/cleanup.
17. Polimorfizm STJ dla `Error` w `ChapterInfo`.
18. Zastąpić `Clone()` przez `MemberwiseClone` lub specjalny typ.
19. Idempotency key w `StartStory` / `/start` endpoint.
20. Uspójnić nazewnictwo i wersję pakietów (Directory.Build.props).

### 🟢 Kosmetyka
21. Polskie komentarze → angielskie.
22. Usunąć redundantne definicje `ChapterId`.
23. `StoryOptions` bindable z `IConfiguration`.
24. Przeparcelować `AdvancedScenariosTests.cs` (856 LOC → 4 pliki).

---

## 13. Werdykt

Pomysł i narracyjne API — **bardzo dobre**, Tale Code jest realnym usprawnieniem czytelności względem Chain/Flow. Implementacja — **prototyp dobry, produkcja nie**. Dokumentacja — **zdezaktualizowana w stopniu, który wprowadza użytkownika w błąd przy pierwszym kontakcie**. ADR — **przeszacowany i niespójny z kodem**.

Bez naprawienia punktów 🔴, framework nie powinien być reklamowany jako „Production Ready”. Po naprawie ✅ — solidne narzędzie do klasy use case'ów „workflow liniowy z pauzą w jednym procesie”.

