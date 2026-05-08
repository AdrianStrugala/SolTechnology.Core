# SolTechnology.Core.Api — Production Readiness Review

**Data:** 2026-05-07
**Zakres:** `src/SolTechnology.Core.Api` (v0.6.0)
**Pliki w paczce:**
- `ModuleInstaller.cs` (+ `ConfigureSwaggerOptions`)
- `Filters/ExceptionFilter.cs` — emits RFC 7807 ProblemDetails
- `Filters/ResultConversionFilter.cs` — **nowy 2026-05-07**: konwertuje `Result<T>` na surowy DTO (success) lub ProblemDetails (failure)
- `Exceptions/ApiExceptionOptions.cs`
- `Exceptions/ApiProblemDetailsFactory.cs` — **nowy 2026-05-07** (zastąpił `ApiErrorFactory`)
- ~~`Filters/ResponseEnvelopeFilter.cs`~~ — **usunięte 2026-05-07** (pivot do ProblemDetails; sekcja 3)
- ~~`Exceptions/ApiErrorFactory.cs`~~ — **usunięte 2026-05-07** (zastąpione przez `ApiProblemDetailsFactory`)
- ~~`Middlewares/ExceptionHandlerMiddleware.cs`~~ — **usunięte 2026-05-07** (filter is preferred; sekcja 4)
- ~~`Middlewares/ResponseEnvelopeResultExecutor.cs`~~ — **usunięte 2026-05-07** (martwy kod; sekcja 5)
- `Testing/APIFixture.cs`
- `SolTechnology.Core.API.csproj`

---

## 0. Podsumowanie zarządcze (TL;DR)

Paczka oferuje rozsądny zestaw klocków (versioning, envelope, exception handler, fixture testowy), ale w obecnej formie **nie nadaje się do użycia produkcyjnego bez poprawek po stronie konsumenta**. Najważniejsze problemy:

1. **Bezpieczeństwo / Information Disclosure** — stack trace wycieka do odpowiedzi HTTP.
2. **Obserwowalność** — logger gubi obiekt wyjątku (loguje sam `Message`), brak `traceId`/correlation.
3. **Złe statusy HTTP** — wszystko nieznane → `400 BadRequest` zamiast `500`. Łamie alerting (4xx vs 5xx) i kontrakty klienckie.
4. **Brak wygody (DX)** — brak `AddApiCore` / `UseApiCore`; konsument sklejający 6 paczek pisze 30+ linii boilerplate (vide `DreamTravel/Program.cs`).
5. **Martwy / mylący kod** — `ResponseEnvelopeResultExecutor` z komentarzem „FILTER is preferred", nigdzie nierejestrowany.
6. **Niespójność nazewnicza** — `Api` / `API` / `APIFixture` w tej samej paczce.
7. **Wyciek zależności testowych** — `Microsoft.AspNetCore.Mvc.Testing` i `TestHost` jadą do każdego produkcyjnego deployu.

Ocena ogólna: **C-**. Po naprawie ~3 dni roboczych — realne **A-**.

---

## 1. `ModuleInstaller.cs` + `ConfigureSwaggerOptions`

### 1.1. Co robi dobrze
- Sensowny, opiniotwórczy default: header-based versioning przez `X-API-VERSION`. Nie zaśmieca URL-i.
- `ReportApiVersions = true` — klienci dostają informację o wersjach w nagłówku odpowiedzi.
- `AssumeDefaultVersionWhenUnspecified` — wstecznie kompatybilne z klientami sprzed wersjonowania.
- Swagger generuje dokument per wersja.

### 1.2. Problemy

**[P1] Brak `AddApiCore` / `UseApiCore` — paczka „nie działa out-of-the-box".**
Aby uzyskać deklarowaną funkcjonalność, konsument musi własnoręcznie:
```csharp
services.AddScoped<ExceptionFilter>();
services.AddScoped<ResponseEnvelopeFilter>();
services.AddControllers(o => { o.Filters.Add<ExceptionFilter>(); o.Filters.Add<ResponseEnvelopeFilter>(); });
// + Swagger UI loop (15 linii)
// + UseSwagger / UseSwaggerUI
```
To dokładnie ten boilerplate, który widać w `DreamTravel.Api/Program.cs` (linie ~100–160). Paczka core powinna sprowadzać go do jednej linii.

**[P2] Brak nadpisywania reader'a / strategii wersjonowania.**
Sygnatura `AddVersioning` jest sztywna. Realne API nierzadko potrzebują:
- fallback na query string (`?api-version=`) dla curla / Postmana,
- innej nazwy nagłówka,
- media-type versioning,
- różnych formatów `GroupNameFormat` (np. `'v'VVV` dla `v1.0`).

Powinno być:
```csharp
AddVersioning(Action<ApiVersioningOptions>? configure = null,
              Action<ApiExplorerOptions>? configureExplorer = null,
              string apiTitle = "API")
```
albo całość przez bound options (`AddVersioning(configuration.GetSection("Api:Versioning"))`).

**[P3] `ConfigureSwaggerOptions` rejestrowane jako `Transient` mimo że jest stateless.**
`Singleton` jest właściwsze i tańsze. `IConfigureOptions<SwaggerGenOptions>` wywołuje się przy każdej budowie `SwaggerGenOptions` — niepotrzebne alokacje.

**[P4] Hardkodowane teksty UI w `ConfigureSwaggerOptions`.**
`"⚠️ This API version is deprecated..."` i `"Current stable API version"` — emoji + brak lokalizacji + brak punktu rozszerzenia. Klient nie ma jak wstrzyknąć własnego `OpenApiInfo` (Contact, License, TermsOfService — wymagane przez wiele bramek API / API Hub).

**[P5] Default `defaultMajorVersion = 2` jest dyskusyjny.**
Większość konsumentów startuje od `1.0`. Domyślna wartość `2.0` jest specyficzna dla DreamTravel i nieintuicyjna dla nowego użytkownika paczki.

**[P6] Brak walidacji `apiTitle` (może być `null` przy wymuszeniu).**
W publicznym API warto `ArgumentException.ThrowIfNullOrWhiteSpace`.

**[P7] Brak konfiguracji `ApiVersionSelector` i `ErrorResponses`.**
Jak klient wyśle nieistniejącą wersję — dostaje `400` z domyślnym body, **nie owiniętym w `Result`**. Łamie kontrakt envelope'u.

### 1.3. Brakujące rozszerzenia
- `UseSwaggerWithVersioning(this IApplicationBuilder, ...)` — pętla po `IApiVersionDescriptionProvider` siedzi w każdym konsumencie.
- `AddApiCore(...)` — opiniotwórcze „wszystko naraz".
- `AddExceptionHandling(Action<ApiExceptionOptions>?)` z opcjami.
- `AddResponseEnvelope(Action<ResponseEnvelopeOptions>?)`.

---

## 2. `Filters/ExceptionFilter.cs`

### 2.1. Problemy

**[KRYT] Logger gubi obiekt wyjątku.**
```csharp
_logger.LogError(context.Exception.Message);
```
Tracimy: typ wyjątku, stack trace, inner exceptions, `Data`. W structured logging (Serilog/Seq/App Insights) oznacza to brak grupowania i brak diagnozowalności. Powinno być:
```csharp
_logger.LogError(context.Exception, "Unhandled exception in {Path}", context.HttpContext.Request.Path);
```

**[KRYT — security] Stack trace w odpowiedzi HTTP.**
`Error.From(exception)` (w `SolTechnology.Core.CQRS`) ustawia `Description = exception.StackTrace`. Filter serializuje to do body. Ujawnianie stack trace'u na produkcji to klasyczny *Information Exposure* (CWE-209). Powinno być za flagą `IncludeExceptionDetails` (włączane jawnie albo automatycznie tylko w `Development`).

**[KRYT] Default 400 BadRequest dla wszystkiego.**
```csharp
int code = (int)HttpStatusCode.BadRequest;
```
Nieobsłużony `NullReferenceException`, `InvalidOperationException`, `DbUpdateException`, `HttpRequestException` — wszystkie dają 400. Konsekwencje:
- Alerting na 5xx nie wykryje awarii backendu.
- Klient (np. retry policy w Polly) zinterpretuje to jako błąd wejścia i nie ponowi.
- SLO/SLI liczone na 5xx są zafałszowane.

Default musi być `500 InternalServerError`. 400 może być **tylko** dla wyjątków, o których wiemy, że są efektem wejścia (`ValidationException`, `ArgumentException`, `JsonException` na bindingu).

**[P1] Bardzo wąska mapa exception → status.**
Tylko `TaskCanceledException` (499) i `ValidationException` (400). Brakuje co najmniej:
- `OperationCanceledException` → 499 (a nie tylko `TaskCanceledException`),
- `UnauthorizedAccessException` → 401,
- `KeyNotFoundException` / własny `NotFoundException` → 404,
- `ArgumentException` / `ArgumentNullException` → 400,
- `NotImplementedException` → 501,
- `TimeoutException` → 504,
- `DbUpdateConcurrencyException` → 409.

Architektonicznie: zamiast `switch` w jednej metodzie — `IExceptionStatusCodeMapper` z domyślną implementacją i możliwością nadpisania per host.

**[P2] `499 Client Closed Request` jest nieformalny.**
To nginx-owe rozszerzenie. Niektóre proxies / load balancery go nie rozumieją. Plus: gdy klient już rozłączył się, **odpowiedź często i tak nie zostanie wysłana** — warto wykryć `HttpContext.RequestAborted.IsCancellationRequested` i nie próbować pisać body.

**[P3] `ValidationException` — `error.Message = "Validation failed"`, `error.Description = exception.Message`.**
`FluentValidation.ValidationException.Message` zawiera wszystkie błędy w jednym stringu. Klient nie dostaje strukturalnej listy błędów per pole. Standard branżowy: `ValidationProblemDetails` (RFC 7807) albo dedykowany `ValidationError` z listą `{ Field, Code, Message }` z `exception.Errors`.

**[P4] Brak `CorrelationId` w odpowiedzi.**
Klient zgłasza bug — zero szans na powiązanie z logami. Powinno być `Result.Error.CorrelationId` zaczytane z `ICorrelationIdService` (z `Core.Logging`), z fallbackiem na `Activity.Current?.TraceId.ToHexString()`.

**Uwaga (anti-pattern):** **nie używać** `HttpContext.TraceIdentifier`. ASP.NET Core zwraca tam `Activity.Current?.Id` (format hierarchical / W3C-id-with-span) albo connection-id Kestrel (`0HMVA9HFCK4O8:00000001`). To **nie jest** ten sam string co `TraceId.ToHexString()` (32-hex), którego używa `Core.Logging` w `BeginScope` i echo'uje na `X-Correlation-Id`. Klient w body dostałby identyfikator którego nie znajdzie w Seq/AppInsights. Źródłem prawdy jest `ICorrelationIdService`.

**[P5] `ExceptionFilter` ≠ `IAsyncExceptionFilter`.**
Synchronous filter — w pipeline'ie Mvc preferowany jest async wariant.

**[P6] Brak obsługi `Response.HasStarted`.**
Jeżeli odpowiedź już zaczęła się zapisywać, `ObjectResult` rzuci. To samo dotyczy middleware'a.

**[P7] `OnException` nie ustawia `Content-Type: application/problem+json` / nie respektuje content negotiation.**
Klient akceptujący XML nadal dostanie JSON.

---

## 3. ~~`Filters/ResponseEnvelopeFilter.cs`~~ — usunięte 2026-05-07 (pivot do ProblemDetails)

**Decyzja produktowa (2026-05-07):** rezygnacja z custom envelope `Result<T>` na granicy HTTP. Paczka pivotuje na **RFC 7807 / RFC 9457 ProblemDetails** — obecny standard `.NET` (od .NET 7+, `services.AddProblemDetails()`, `[ApiController]` auto-konwersja, native OpenAPI schema, rozumiane przez Polly / Refit / App Insights / Aspire).

### 3.1. Co znika
- `ResponseEnvelopeFilter.cs` — usunięte.
- `Exceptions/ApiErrorFactory.cs` — usunięte (budowało `Error` z opcjonalnym diagnostic; zastąpione przez `ApiProblemDetailsFactory`).
- Wszystkie problemy z sekcji 3.1 oryginalnego review (skipy, mapowanie ProblemDetails → Error, JsonResult, schema-OpenAPI mismatch, status inference) — **przestają istnieć**, bo nie ma envelope filtra.
- Kontrakt klientów: zamiast deserializować `{ isSuccess, data, error }` deserializują kanoniczne `T` (sukces) albo `ProblemDetails` (błąd).

### 3.2. Co pojawia się w zamian

#### `Filters/ResultConversionFilter.cs` (nowy)
`IAsyncResultFilter` nasłuchujący na `ObjectResult { Value: Result | Result<T> | Error }`:
- `Result<T> { IsSuccess: true, Data }` → `200 OK` z **surowym `Data`** w body. Brak envelope.
- `Result { IsSuccess: true }` (non-generic) → `204 No Content`.
- `Result<T> { IsSuccess: false, Error }` lub `Result { Error }` → `ProblemDetails` ze statusem z `Error.StatusCode` (default `500`).
- Akcja zwróciła bezpośrednio `Error` (np. `BadRequest(error)`) → `ProblemDetails` ze statusem z `objectResult.StatusCode` ?? `error.StatusCode` ?? `500`.
- Inne typy (DTO, primitives, `IActionResult` typu `FileStreamResult`/`NoContentResult`/etc.) — **bez ingerencji**. Skipy wczorajsze (FileResult/EmptyResult/Redirect/Challenge…) są niepotrzebne — filter w ogóle nie dotyka tych ścieżek.

#### `Exceptions/ApiProblemDetailsFactory.cs` (nowy)
Buduje `Microsoft.AspNetCore.Mvc.ProblemDetails`:
- `FromException(exception, statusCode, correlationId, options)` — używane przez `ExceptionFilter`. `ValidationException` automatycznie produkuje `ValidationProblemDetails` ze strukturalnym `errors` per pole (rozwiązuje sec 2 [P3] „strukturalna lista błędów" i should-have #13 jednym ruchem).
- `FromError(error, statusCode, correlationId)` — używane przez `ResultConversionFilter`. Bez gałęzi diagnostic (nie ma exception, nie ma stack trace do leak'u).
- Stałe `Extensions["correlationId"]` (zawsze, gdy id != null) i `Extensions["exception"]` (tylko przy `IncludeExceptionDetails`).
- `type` URI mapowane do RFC 9110 / 4918 dla popularnych statusów; fallback `https://httpstatuses.io/{status}`.
- Content-Type: `application/problem+json` (RFC 7807 §3).

#### Zmiana w `Core.CQRS.Errors`
Dodane subtypy `Error` jako semantyczne markery domain → transport (rozwiązuje też should-have #12 w jednym ruchu):

| Subtyp | Mapowanie HTTP | Body |
|---|---|---|
| `NotFoundError` | 404 | `ProblemDetails` |
| `ConflictError` | 409 | `ProblemDetails` |
| `ValidationError` | 400 | `ValidationProblemDetails` (`errors` per field) |
| `UnauthorizedError` | 401 | `ProblemDetails` |
| `ForbiddenError` | 403 | `ProblemDetails` |
| bazowy `Error` / nieznany subtyp | 500 | `ProblemDetails` |

`ValidationError` niesie `IReadOnlyDictionary<string, string[]> Errors` (per-field). Filter (`ResultConversionFilter`) w ogóle nie wie o HTTP — przekazuje `Error` do `ApiProblemDetailsFactory.FromError`, factory wybiera status po typie.

**Świadomie cofnięte (2026-05-07):** wcześniejsze `int? StatusCode` na `Error` było leak'iem HTTP do warstwy CQRS. Subtypy zostawiają `Core.CQRS` neutralne — handler mówi „nie znalazłem", nie „zwróć 404". Ten sam `Result` reusable poza HTTP (gRPC: `StatusCode.NotFound`, message bus: skip-and-ack itd.).

#### Zmiana w `ExceptionFilter`
Mapped exception → `new ObjectResult(problemDetails) { ContentTypes = { "application/problem+json" } }`. `Result` envelope całkiem zniknął z tej ścieżki.

#### Zmiana w `AddApiExceptionHandling`
Rejestruje teraz: `AddCoreLogging`, `AddProblemDetails` (Microsoft framework integration), `AddOptions<ApiExceptionOptions>`, `AddScoped<ExceptionFilter>`, **`AddScoped<ResultConversionFilter>`** (nowe).

### 3.3. Konsekwencje dla pozostałych problemów z review

| Problem | Status |
|---|---|
| 3.1 [KRYT] ProblemDetails wrap przez `.ToString()` | **N/A** — filter usunięty |
| 3.1 [P1] Brak skipów (FileResult, EmptyResult, …) | **N/A** — filter usunięty |
| 3.1 [P2] Rozróżnienie `is Result` vs ProblemDetails | **N/A** |
| 3.1 [P3] `IResultFilter` → `IAsyncResultFilter` | **rozwiązane** w `ResultConversionFilter` — od razu async |
| 3.1 [P4] `JsonResult` ominięty | **N/A** — filter usunięty, MVC `JsonResult` przechodzi netto |
| 3.1 [P5] Schema OpenAPI nie zgadza się | **N/A** — ProblemDetails ma natywny schemat OpenAPI, sukces zwraca surowy DTO (zgodny z `ProducesResponseType<T>`) |
| 3.1 [P6] `Result.Fail` w `Ok(...)` daje 200 | **rozwiązane** — `ResultConversionFilter` wnioskuje status ze stanu `Result` |
| Should-have #13 (`ValidationException` strukturalnie) | **rozwiązane** — `ValidationProblemDetails.Errors` |
| Should-have #10 (`[SkipResponseEnvelope]`) | **N/A** — nie ma envelope'u |
| Nice-to-have #16 (Schema filter na `Result<T>`) | **N/A** |
| Sec 9.5 (`ProblemDetailsExceptionFilter`) | **stało się głównym wzorcem** — zostało wpięte jako default |

---

## 4. ~~`Middlewares/ExceptionHandlerMiddleware.cs`~~ — usunięte 2026-05-07

**Decyzja:** plik usunięty. Komentarz w kodzie sam to mówił: „FILTER is preferred". Utrzymywanie dwóch ścieżek z _różną_ logiką mapowania (drift) i jednym jako safety-net napisanym mniej dokładnie było gorsze niż brak safety-net'u.

**Co konsekwentnie tracimy:**
- Wyjątki z auth / routing / innych user-level middleware'ów **przed** MVC pipeline'em nie są opakowywane w `Result` — lecą do hosta.
- W praktyce: w Development `app.UseDeveloperExceptionPage()` pokazuje pełny detal; w Production `app.UseExceptionHandler("/error")` (lub default ASP.NET) zwraca generic 500.
- Wyjątki z model bindingu są i tak obsługiwane przez MVC (zwraca `400 ProblemDetails`) niezależnie od naszych filtrów.

**Co świadomie zyskujemy:**
- Jedna ścieżka mapowania → zero drifu.
- Filozofia A+E z #2 jest spójna: paczka mapuje to, co zna; reszta to nie jej rola, host decyduje.
- Mniej publicznego API surface paczki.
- Wycina się problem `Response.HasStarted` (#6 z must-have) — nie dotyczy filtra MVC, który działa _przed_ pisaniem response.
- Wycina się problem `WriteAsJsonAsync` bez `IOptions<JsonOptions>` z DI (filter używa MVC `ObjectResult`, więc dziedziczy konfigurację `AddControllers().AddJsonOptions(...)` darmowo).

**Migracja konsumentów:** żaden konsument w repo nie wpinał `app.UseMiddleware<ExceptionHandlerMiddleware>()`. DreamTravel.Api używa tylko filtra. Brak breaking change w praktyce.

---

## 5. ~~`Middlewares/ResponseEnvelopeResultExecutor.cs`~~ — usunięte 2026-05-07

**Decyzja:** plik usunięty razem z `ExceptionHandlerMiddleware`. Klasa była publiczna, dziedziczyła z `ObjectResultExecutor`, **nigdzie nie rejestrowana**, opatrzona własnym komentarzem „FILTER is preferred" — czysto martwy kod. Refleksja na `Result<>` per response, niespójność z filtrem (`TypeCode.Object` gating), ryzyko `NullReferenceException` (`!` po `GetProperty`).

`ResponseEnvelopeFilter` pokrywa 100% sensownych przypadków taniej.

**Migracja:** żaden konsument w repo nie używał executor-a (zero referencji w `git grep`).

---

## 6. `Testing/APIFixture.cs`

### 6.1. Problemy

**[P1] Wyciek `Microsoft.AspNetCore.Mvc.Testing` + `TestHost` do każdej produkcyjnej aplikacji konsumenta.**
Te pakiety dociągają `WebApplicationFactory`, `TestServer`, dodatkowe assembly Microsoft.AspNetCore.* dev-only. Każdy konsument paczki API ma je w `bin/Release` na produkcji. To:
- Powiększa surface ataku (CVE w `TestHost` → CVE w produkcyjnym deployu),
- Powiększa rozmiar publish-output,
- Sprzecznie semantycznie (paczka „API" zaciąga „Testing").

**Rekomendacja:** wydzielenie `SolTechnology.Core.Api.Testing` jako osobnego NuGet (z `<DevelopmentDependency>true</DevelopmentDependency>`).

**[P2] Niespójna nazwa `APIFixture` vs reszta paczki.**
Konwencja .NET (`Microsoft.AspNetCore.Mvc` itp.) to PascalCase z akronimami: `Api`, `Url`, `Json`. `APIFixture` jest wyjątkiem.

**[P3] `services.AddLogging(l => l.AddConsole())` wymuszone.**
Testy często używają xUnit `ITestOutputHelper` lub `LoggerFactory` z capture'em. Console + duplikat z xUnit zaśmieca output. Powinno być przez delegata albo opcjonalne.

**[P4] Brak `IAsyncDisposable`.**
`HttpClient.Dispose` i `TestServer.Dispose` mają warianty async; `WebApplicationFactory.DisposeAsync` jest preferowany w nowoczesnych testach.

**[P5] Brak ekspozycji `WebApplicationFactory<TEntryPoint>`.**
Wiele real-world testów potrzebuje:
- `factory.WithWebHostBuilder(...)` per test (np. zamiana DbContext na in-memory innym dla każdego testu),
- nazwanego klienta z innymi handlerami (`HandleCookies`, `AllowAutoRedirect=false`),
- ustawienia `Environment` na `Testing`,
- ustawienia content root.

Dziś dostajemy tylko jeden `HttpClient` i już zbudowany `TestServer`. Re-konfiguracja niemożliwa po fakcie.

**[P6] Konstruktor robi I/O (build host).**
W xUnit z fixture sharing OK, ale dla `IClassFixture` to ślepy reset między testami. Brak metody `WithServices(...)` / `CreateClient(Action<...>)`.

**[P7] `TestServer.PreserveExecutionContext = true` po `CreateClient()`.**
Dokumentacja zaleca ustawić **przed** stworzeniem klienta. Tu kolejność akurat OK (`CreateClient` jest wołany po), ale to subtelność, w której łatwo zrobić regres.

**[P8] `IDisposable` bez `GC.SuppressFinalize`.**
Drobiazg.

---

## 7. `SolTechnology.Core.API.csproj`

**[P1] Niespójność nazw:**

| Element | Wartość |
|---|---|
| Folder | `SolTechnology.Core.Api` |
| Plik projektu | `SolTechnology.Core.API.csproj` |
| `AssemblyName` / `PackageId` | `SolTechnology.Core.Api` |
| `RootNamespace` (default) | `SolTechnology.Core.API` |
| Namespaces w kodzie | `SolTechnology.Core.API` |
| Type | `APIFixture` |

Nowy konsument nie wie czego się trzymać. Trzeba ujednolicić — preferowane: `Api` (PascalCase, jak `Sql`/`Auth` w tej samej solucji), z aliasem namespace dla wstecznej kompatybilności.

**[P2] Brak `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.**
W kodzie są XML-doc comments (`<summary>`), ale paczka NuGet ich nie eksportuje → IntelliSense u konsumenta jest pusty.

**[P3] Brak `<Nullable>enable</Nullable>` i `<ImplicitUsings>enable</ImplicitUsings>`.**
Reszta repo (np. CQRS) ma `#nullable enable` per plik, ale paczka publiczna nie wymusza nullability. Konsekwencja: `Result.Error` jest deklarowany jako `Error?`, ale niektóre miejsca traktują go jak non-null bez sprawdzenia.

**[P4] `Microsoft.AspNetCore.Mvc.Testing` 10.0.0 i `Microsoft.AspNetCore.TestHost` 10.0.0 jako runtime dependency.**
Patrz pkt 6.1 [P1].

**[P5] Brak `TargetFramework` w plikach (dziedziczy z root).**
OK, ale warto sprawdzić multi-target (`net8.0;net9.0;net10.0`) by paczka działała na hostach LTS. Obecnie wymusza najnowszy.

**[P6] Brak `MinClientVersion`, `PackageLicenseExpression`, `PackageProjectUrl`.**
Dla publicznego NuGet to wymóg sklepowy + wymóg supply-chain (SBOM, OSS compliance).

---

## 8. Zależności od `SolTechnology.Core.CQRS` i `SolTechnology.Core.Logging`

**Decyzja produktowa (2026-05-07):** `Core.Api` zachowuje **hard dependency** na `Core.CQRS` (kontrakt envelope `Result`/`Result<T>`/`Error`) i dodaje **hard dependency** na `Core.Logging` (correlation, scope enrichment, request logging). Ujednolicenie stack'u jest świadome — paczki tworzą spójny zestaw, a nie luźny worek niezależnych klocków. Konsument biorący `Core.Api` dostaje cały standard obserwowalności i wzorców błędów „za darmo".

### 8.1. Konsekwencje hard-dep na `Core.Logging`
- `ExceptionFilter` / `ExceptionHandlerMiddleware` mogą **wstrzykiwać `ICorrelationIdService`** z DI (bez fallback gymnastyki na `Activity.Current`, choć w razie braku Logging-middleware w pipeline usługa wygeneruje id ad-hoc).
- `Error.CorrelationId` (nowe pole) wypełnione spójnie z log scope (`CorrelationId` w `BeginScope`) i z nagłówkiem `X-Correlation-Id` na response — klient ma jeden ciąg znaków który prowadzi do logów.
- `UseApiCore` może (i powinno) zarejestrować `UseCoreLogging` automatycznie, w prawidłowej kolejności (przed exception handler-em).
- Eliminujemy duplikat: nie potrzeba własnego `CorrelationIdMiddleware` w Api.

### 8.2. Konsekwencje hard-dep na `Core.CQRS`
- `Result<T>` pozostaje **wspólnym kontraktem** envelope'u na granicy HTTP. Klienci deserializujący odpowiedź referencują `Core.CQRS` (lub generują typy z OpenAPI) — to akceptowane.
- Wciąż otwarty problem: **`Error.Description = StackTrace`** w `Core.CQRS` jest design-flaw. Naprawa po stronie API (filtrowanie / `IncludeExceptionDetails`) jest workaroundem; docelowo `Error` w CQRS musi mieć osobne pole `Details` / `Exception` rozdzielone od user-facing `Description`. Patrz #21 w sekcji 12.
- `Error` zyskuje nowe pole `CorrelationId` (string?). Zmiana niebreaking (nullable, JsonIgnore-when-null), ale wymaga edytu `Core.CQRS`.

### 8.3. Co zostaje uważne
- `Core.Api` **nie powinno** zaciągać tranzytywnie do `Core.CQRS` rzeczy z MediatR / pipeline behaviors konsumenta (handler-discovery itp.). Sam typ `Result` + `Error` wystarczy. Jeśli `Core.CQRS` w obecnej formie ciągnie więcej niż trzeba — to argument za wydzieleniem `Core.CQRS.Abstractions` (out-of-scope tego review).

---

## 9. Brakujące, oczekiwane elementy paczki

Lista rzeczy, których realny zespół oczekuje od „core API" w 2026:

1. **Integracja z `Core.Logging` w defaultach** — `UseApiCore` musi wpiąć `UseCoreLogging` przed exception handler-em; `ExceptionFilter` / `ExceptionHandlerMiddleware` muszą używać `ICorrelationIdService` jako źródła `Error.CorrelationId`. **Nie** reimplementujemy CorrelationIdMiddleware (już jest w `Core.Logging`, w pełnej formie W3C Trace Context z echo `X-Correlation-Id` + `traceparent`).
2. **Health checks endpoint** — `app.MapHealthChecks("/health")` z domyślnym formatterem zwracającym `Result`.
3. **Rate limiting wrapper** — opcjonalnie, z `Microsoft.AspNetCore.RateLimiting`.
4. **CORS helper** — w `DreamTravel/Program.cs` widać 30 linii ręcznego CORS-a; powinno być `AddDefaultCors(configuration.GetSection("Cors"))`.
5. **`ProblemDetailsExceptionFilter`** — alternatywa dla envelope'u dla zespołów wybierających RFC 7807.
6. **OpenAPI: examples + security schemes** — `AddSwaggerGen` wewnętrznie z opcjami.
7. **Schema filter na `Result<T>`** — żeby Swagger pokazał właściwy kształt odpowiedzi.
8. **Domain → HTTP exception types** (`NotFoundException`, `ConflictException`, `ForbiddenException`) z preregistrowanym mapowaniem.
9. **Endpoint logger** — to co `DreamTravel/Program.cs` robi ręcznie (`LogAvailableEndpoints`) — gotowiec.
10. **Koordynacja log-levelu z `LoggingMiddleware`** — `LoggingMiddleware` w `finally` loguje finish ze status-aware levelem (5xx→Error, 4xx→Warning). Filter/middleware z Api dla mapped exception (4xx) powinny logować na `Warning`, dla unmapped (5xx) na `Error` — żeby uniknąć dwóch wpisów Error per incydent (jeden z Api ze stack-trace, drugi z Logging z `ElapsedMs`).

---

## 10. Testy

Stan: **brak testów dla `SolTechnology.Core.Api`**. W folderze `tests/` jest `SolTechnology.Core.HTTP.Tests`, `Logging.Tests`, `Sql.Tests`, ale **nie ma `Api.Tests`**.

Kandydaci na testy (po refaktorze):
- `ExceptionFilter`: każda gałąź mapowania exception → status + `IncludeExceptionDetails` on/off + brak stack trace w body.
- `ResponseEnvelopeFilter`: skip cases (FileResult, ProblemDetails, 204, [SkipEnvelope]) + happy path + już-owinięte-w-Result.
- `ExceptionHandlerMiddleware`: `Response.HasStarted == true`, exception po starcie, `OperationCanceledException` gdy `RequestAborted`.
- `AddVersioning`: rejestracja, swagger doc per wersja, deprecated header w odpowiedzi.
- `APIFixture`: lifetime, environment override, custom services.

---

## 11. Bezpieczeństwo / Hardening — tabela

| Problem | Severity | Lokalizacja |
|---|---|---|
| ~~Stack trace w body~~ — **rozwiązane 2026-05-07** przez `ApiErrorFactory` + `IncludeExceptionDetails` opt-in. | **High** (CWE-209) | `ExceptionFilter`, `ExceptionHandlerMiddleware` (przez `Error.From`) |
| ~~`LogError(message)` zamiast `LogError(ex, ...)`~~ — **rozwiązane 2026-05-07** | Medium (operacyjny) | jw. |
| ~~Default 400 ukrywa 5xx~~ — **rozwiązane 2026-05-07**: A+E. Brak domyślnego statusu, nieznane wyjątki rethrow'owane do hosta z `LogCritical`. | High (operacyjny / SLO) | jw. |
| ~~Brak `CorrelationId` w błędzie~~ — **rozwiązane 2026-05-07** przez `Error.CorrelationId` + `ICorrelationIdService` z `Core.Logging`. | Medium | jw. |
| ~~Brak rozróżnienia `OperationCanceledException` produced by `RequestAborted`~~ — **rozwiązane 2026-05-07**: dedykowana gałąź client-abort, brak loga. | Low (noise w logach) | jw. |
| ~~`WriteAsJsonAsync` bez `JsonOptions` z DI~~ — **N/A 2026-05-07**: middleware usunięty. | Low (kontrakt) | ~~middleware~~ |
| Brak skipu `FileResult` w envelope | Medium (uszkodzone download'y) | filter |
| Hardkodowany komunikat o deprecation w Swagger | Low | `ConfigureSwaggerOptions` |

---

## 12. Priorytetyzowana lista zmian (proponowana kolejność)

### Must-have (blocker produkcyjny)
1. ~~Naprawa logowania: `LogError(exception, ...)`.~~ ✅ rozwiązane 2026-05-07.
2. ~~Default status code → 500.~~ **Reframed 2026-05-07: A+E (rethrow + LogCritical) zamiast defaultu.** Filter/middleware mapują tylko znane typy (obecnie: `ValidationException` w filtrze; mapa rośnie w should-have #9). Nieznane typy → `LogCritical` z templatem „Unmapped exception of type {ExceptionType} ... — rethrowing to host pipeline" + rethrow do hosta (DeveloperExceptionPage w dev, generic 500 w prod, lub custom `UseExceptionHandler`). Klient-abort (OperationCanceledException + `RequestAborted.IsCancellationRequested`) wykrywany odrębnie i rethrow'owany **bez** loga (`LoggingMiddleware` z `Core.Logging` zaloguje finish na Warning). Paczka nie zgaduje semantyki 500 vs 503 vs 504 — to zostaje decyzją hosta / przyszłego mappera. ✅ rozwiązane 2026-05-07.
3. ~~Stack trace tylko w `Development` (lub za flagą `IncludeExceptionDetails`).~~ **Rozwiązane 2026-05-07.** Wprowadzony `ApiExceptionOptions.IncludeExceptionDetails` (default `false`), `ApiErrorFactory.Build(...)` jako bezpieczny zamiennik dla `Error.From(exception)` (ten ostatni nieusunięty z CQRS, ale nie używany już przez Api). Filter i middleware czytają opcje przez `IOptions<ApiExceptionOptions>`. Nowy installer `services.AddApiExceptionHandling(o => o.IncludeExceptionDetails = env.IsDevelopment())`.
4. ~~**`Error.CorrelationId`** — nowe pole na `Error` (w `Core.CQRS`), wypełniane w `ExceptionFilter`/`ExceptionHandlerMiddleware` z `ICorrelationIdService` (hard-dep na `Core.Logging`). Spójne z `X-Correlation-Id` na response i `CorrelationId` w log scope. **Nie** używać `HttpContext.TraceIdentifier`.~~ **Rozwiązane 2026-05-07.** Dodane: `Error.CorrelationId` w `Core.CQRS`; `ProjectReference` z `Core.Api` na `Core.Logging`; `ExceptionFilter` i `ExceptionHandlerMiddleware` wstrzykują `ICorrelationIdService` i wołają `GetOrGenerate().Value` (idempotentnie tworzy id jeśli pipeline `UseCoreLogging` nie był jeszcze; wartość zsynchronizowana z W3C `Activity.Current.TraceId`); `ApiErrorFactory.Build(...)` przyjmuje `correlationId` jako wymagany parametr; `AddApiExceptionHandling` woła idempotentne `services.AddCoreLogging()` żeby zarejestrować service, gdy konsument jeszcze nie wpiął loggingu.
5. ~~Skip `ProblemDetails` / `FileResult` / `EmptyResult` w envelope; mapowanie `ProblemDetails` → `Error`.~~ **Reframed 2026-05-07: pivot do RFC 7807 ProblemDetails.** `ResponseEnvelopeFilter` usunięty, zastąpiony `ResultConversionFilter` (auto-konwersja `Result<T>` → surowy DTO/ProblemDetails) + `ApiProblemDetailsFactory`. Skipy stają się N/A — filter nie dotyka `FileResult`/`EmptyResult`/`ProblemDetails` ani innych nie-`Result` ścieżek. ✅ rozwiązane 2026-05-07. Patrz sekcja 3.
6. ~~Sprawdzenie `Response.HasStarted` w middleware.~~ **N/A 2026-05-07** — middleware usunięty (sekcja 4); filter MVC nie pisze response bezpośrednio, problem nie istnieje.
7. ~~Usunięcie / `[Obsolete]` na `ResponseEnvelopeResultExecutor`.~~ **Rozwiązane 2026-05-07: plik usunięty** (sekcja 5).

### Should-have (DX + dojrzałość)
8. `AddApiCore`, `UseApiCore` (woła `UseCoreLogging` przed exception handler-em), `UseSwaggerWithVersioning`.
9. `IExceptionStatusCodeMapper` + rozszerzona mapa typów wyjątków.
10. ~~`[SkipResponseEnvelope]` atrybut.~~ **N/A 2026-05-07** — pivot do ProblemDetails, brak envelope'u do skip'owania.
11. Koordynacja log-levelu z `LoggingMiddleware` (mapped 4xx → `Warning`, unmapped → `Error`) — patrz #9.10. Eliminuje duplikat „dwa Error per incydent".
12. ~~`ApiException` / `NotFoundException` / `ConflictException` itp. (domain types).~~ **Reframed + rozwiązane 2026-05-07.** Po pivocie do `Result` pattern + ProblemDetails, paczka idzie ścieżką **`Error` subtypów** (nie exception types): `NotFoundError`, `ConflictError`, `ValidationError`, `UnauthorizedError`, `ForbiddenError` w `Core.CQRS.Errors`. Handler returnuje `Result<T>.Fail(new NotFoundError(...))`; `ApiProblemDetailsFactory.FromError` switch'uje po typie do statusu HTTP. Przy okazji wycofany `Error.StatusCode` (był leak'iem HTTP do CQRS).
13. ~~`ValidationException` → strukturalna lista błędów.~~ **Rozwiązane 2026-05-07** — `ApiProblemDetailsFactory` produkuje `ValidationProblemDetails` ze strukturalnym `errors` per pole.
14. Wydzielenie `SolTechnology.Core.Api.Testing` jako osobna paczka.
15. Async warianty filtrów (`IAsyncExceptionFilter`, `IAsyncResultFilter`).

### Nice-to-have
16. ~~Schema filter dla Swaggera owijający responses w `Result<T>`.~~ **N/A 2026-05-07** — sukces zwraca surowy DTO (zgodny z `[ProducesResponseType<T>]`), błąd zwraca `ProblemDetails` (natywny schemat OpenAPI). Brak envelope = brak schema mismatch.
17. `AddDefaultCors`, healthchecks, endpoint logger.
18. Pakiet kontraktów (`SolTechnology.Core.Api.Contracts`).
19. Multi-targeting `net8.0;net9.0;net10.0`.
20. Przerwa nazewnicza `Api` vs `API` (z deprecated namespace alias).

### Out-of-package, ale powiązane
21. `Error.Description ≠ StackTrace` w `Core.CQRS` (osobne pole `Details` lub `Exception`).
22. Doc `docs/Api.md`: minimalny snippet zamiast kopiowania pętli swagger UI.

---

## 13. Wstępna ocena pracochłonności

| Pakiet zmian | Czas |
|---|---|
| Must-have (1–7) | ~0.5 dnia |
| Should-have (8–15) | ~1.5 dnia |
| Nice-to-have (16–20) | ~1 dzień |
| Out-of-package (21–22) | ~0.5 dnia |
| **Razem** | **~3.5 dnia** roboczego |

---

## 14. Otwarte pytania do dyskusji (blokery decyzyjne)

1. ~~**Envelope vs ProblemDetails**~~ — **rozstrzygnięte 2026-05-07: pivot do RFC 7807 ProblemDetails.** `ResponseEnvelopeFilter` usunięty; `ResultConversionFilter` rozpakowuje `Result<T>` (sukces) na surowy DTO i konwertuje fail na `ProblemDetails`. Schema OpenAPI zgodny z `ProducesResponseType<T>` + natywne wsparcie dla `ProblemDetails`. Standard `.NET 7+` / RFC 9457.
2. ~~Czy `Result` / `Error` powinny zostać wspólnym kontraktem CQRS+API~~ — **rozstrzygnięte 2026-05-07: tak, hard-dep na `Core.CQRS` zostaje, `Result`/`Error` to wspólny kontrakt envelope'u; `Error` zyskuje pole `CorrelationId`.**
3. **Wersjonowanie** — zostać przy header-only, czy dodać query/url/media-type jako wybierane opcjonalnie?
4. **Czy wydzielać `Testing` do osobnej paczki teraz**, czy zostawić in-package z `<DevelopmentDependency>`?
5. **Co z nazwą — `Api` czy `API`?** Każdy wariant to breaking change dla kogoś. Decyzja produktowa.
6. **Czy paczka ma startować od v1.0.0** po refaktorze (jako sygnał stabilności), czy iść 0.7.0 → 0.8.0?
7. **Domain exceptions** — `NotFoundException` itd. w `Core.Api` czy w nowej `Core.Domain`?
8. ~~Hard / soft dep na `Core.Logging`~~ — **rozstrzygnięte 2026-05-07: hard-dep, `UseApiCore` wpina `UseCoreLogging` w prawidłowej kolejności.**

---











