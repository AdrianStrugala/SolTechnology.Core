# Plan Implementacji: Story Framework

> **Status:** 🟢 In Progress - Week 1
> **Start Date:** 2024-12-23
> **Last Updated:** 2024-12-24
> **Target Completion:** 2026-01-20 (4 tygodnie)

## 🎯 Cel

Utworzenie zunifikowanego frameworka "Story" łączącego obecne wzorce Chain (CQRS) i Flow w jeden, prosty i czytelny system orkiestracji wieloetapowych procesów biznesowych.

## 📖 Nomenklatura (Tale Code Philosophy)

- **Story** (framework) - opowiedziana sekwencja kroków biznesowych
- **Chapter** (krok) - pojedynczy rozdział historii
- **Narration** (kontekst) - narracja przepływająca przez rozdziały
- **TellStory()** - metoda definiująca sekwencję rozdziałów

```csharp
public class SaveCityStory : StoryHandler<SaveCityInput, SaveCityNarration, SaveCityResult>
{
    protected override async Task TellStory()
    {
        await Chapter<LoadExistingCity>();
        await Chapter<AssignAlternativeName>();
        await Chapter<IncrementSearchCount>();
        await Chapter<SaveToDatabase>();
    }
}
```

## 🏗️ Architektura

### Decyzja: Osobny Projekt (SolTechnology.Core.Story)

**SolTechnology.Core.Flow** zostanie zastąpiony przez **SolTechnology.Core.Story**.

**Uzasadnienie:**
- Flow już ma infrastrukturę (persistence, API, controller)
- Zachowujemy osobny pakiet dla workflow orchestration
- Chain w CQRS pozostaje jako lightweight option (bez dodatkowej zależności)
- Separacja odpowiedzialności: CQRS (patterns) vs Story (orchestration)
- Łatwiejsza adopcja - użytkownicy wybierają co potrzebują

### Struktura Projektu

```
src/SolTechnology.Core.Story/           # NOWY PROJEKT (ex-Flow)
├── SolTechnology.Core.Story.csproj
├── StoryHandler.cs
├── Narration.cs
├── IChapter.cs
├── Chapter.cs
├── InteractiveChapter.cs
├── StoryOptions.cs
├── StoryEngine.cs
├── ModuleInstaller.cs
├── Persistence/
│   ├── IStoryRepository.cs
│   ├── InMemoryStoryRepository.cs
│   └── SqliteStoryRepository.cs
├── Models/
│   ├── StoryInstance.cs
│   ├── ChapterInfo.cs
│   ├── StoryStatus.cs
│   └── DataField.cs
├── Orchestration/
│   └── StoryManager.cs
└── Api/
    └── StoryController.cs
```

## 📝 Checklist Implementacji

### Week 1: Core Framework (Priorytet 1)

- [x] **Setup Projektu** ✅
  - [x] Utworzenie `src/SolTechnology.Core.Story/SolTechnology.Core.Story.csproj`
  - [x] Dodanie referencji do `SolTechnology.Core.CQRS` (Result, Error)
  - [x] Dodanie zależności: Microsoft.Data.Sqlite, Microsoft.AspNetCore.Mvc.Core
  - [x] Aktualizacja `SolTechnology.Core.slnx` (usunąć Flow, dodać Story)

- [x] **Core Abstractions** ✅ (partial)
  - [ ] `StoryHandler<TInput, TNarration, TOutput>` - bazowy handler 🚧
  - [x] `Narration<TInput, TOutput>` - bazowy kontekst
  - [x] `IChapter<TNarration>` - interfejs rozdziału
  - [x] `Chapter<TNarration>` - bazowa klasa rozdziałów
  - [x] `InteractiveChapter<TNarration, TChapterInput>` - bazowa klasa interaktywnych rozdziałów
  - [x] `StoryOptions` - konfiguracja (Default, WithInMemoryPersistence, WithSqlitePersistence)

- [x] **Models** ✅
  - [x] `StoryInstance` - persisted story state
  - [x] `ChapterInfo` - chapter execution tracking
  - [x] `StoryStatus` - enum
  - [x] `DataField + SchemaBuilder` - input schema introspection

- [x] **Persistence Stubs** ✅
  - [x] `IStoryRepository` - interface
  - [x] `InMemoryStoryRepository` - complete implementation
  - [x] `SqliteStoryRepository` - stub (Week 3)

- [x] **Engine & Handler** ✅
  - [x] `StoryEngine` - internal orchestration
  - [x] `StoryHandler<TInput, TNarration, TOutput>` - bazowy handler

- [x] **Registration** ✅
  - [x] `ModuleInstaller.cs` - `RegisterStories()` z auto-discovery
  - [ ] Testy rejestracji (Week 1 Part 2)

- [x] **Basic Tests** ✅ (Week 1 Part 2)
  - [x] `tests/SolTechnology.Core.Story.Tests/StoryHandlerTests.cs` - podstawowa funkcjonalność
  - [x] `tests/SolTechnology.Core.Story.Tests/ChapterTests.cs` - wykonanie rozdziałów
  - [x] `tests/SolTechnology.Core.Story.Tests/InteractiveChapterTests.cs` - interactive chapter behavior
  - [x] Test project created and added to solution

### Week 2: Persistence & Engine (Priorytet 2)

- [x] **Models** ✅ (completed in Week 1)
  - [x] `Models/StoryInstance.cs`
  - [x] `Models/ChapterInfo.cs`
  - [x] `Models/StoryStatus.cs` (enum)
  - [x] `Models/DataField.cs` + SchemaBuilder

- [x] **StoryEngine** ✅ (basic implementation completed in Week 1)
  - [x] Podstawowa orkiestracja kroków
  - [x] Agregacja błędów (AggregateError)
  - [x] Obsługa InteractiveChapter (pause/resume) - basic detection
  - [x] Pomijanie kroków podczas wznawiania - basic structure
  - [x] CancellationToken support

- [x] **Persistence** ✅
  - [x] `Persistence/IStoryRepository.cs`
  - [x] `Persistence/InMemoryStoryRepository.cs`
  - [x] Integracja z StoryEngine (save/load state) ✅

- [x] **Orchestration** ✅
  - [x] `Orchestration/StoryManager.cs` - high-level start/resume API

- [x] **Tests** ✅
  - [x] `InteractiveChapterTests.cs` - schemat inputu, wykonanie z inputem (8 tests)
  - [x] `StoryEngineTests.cs` - orkiestracja, agregacja błędów (9 tests)
  - [x] `ErrorHandlingTests.cs` - Result, AggregateError (10 tests)
  - [x] `InMemoryRepositoryTests.cs` - CRUD, thread-safety (14 tests)
  - [x] `PauseResumeIntegrationTests.cs` - end-to-end pause/resume (7 tests)

### Week 3: Advanced & Migration (Priorytet 3)

- [ ] **SQLite Persistence**
  - [ ] `Persistence/SqliteStoryRepository.cs`
  - [ ] Database schema + migrations
  - [ ] Serializacja/deserializacja context
  - [ ] `SqliteRepositoryTests.cs`

- [ ] **Orchestration**
  - [ ] `Orchestration/StoryManager.cs`
  - [ ] StartStory, ResumeStory, GetStoryState
  - [ ] `StoryManagerTests.cs`

- [ ] **Migracja DreamTravel**
  - [ ] CalculateBestPath: Handler + 5 chapters (InitiateContext, DownloadRoadData, FindProfitablePath, SolveTsp, FormResult)
  - [ ] SampleOrderWorkflow: Handler + 3 chapters (RequestUserInput, ProcessPayment, FetchShippingEstimate)
  - [ ] SaveCityStory: Nowa implementacja + 4 chapters (LoadExistingCity, AssignAlternativeName, IncrementSearchCount, SaveToDatabase)
  - [ ] Aktualizacja ModuleInstaller w DreamTravel (RegisterStories)

- [ ] **Integration Tests**
  - [ ] Migracja CalculateBestPath (weryfikacja wyników)
  - [ ] Migracja SampleOrderWorkflow (pause/resume)
  - [ ] SaveCityStory end-to-end (z Testcontainers)

### Week 4: API, Docs & Cleanup (Priorytet 4)

- [ ] **REST API**
  - [ ] `Api/StoryController.cs` (abstract)
  - [ ] Endpoints: start, resume, get state, get result
  - [ ] Integration test dla API

- [ ] **Deprecation**
  - [ ] `[Obsolete]` na `ChainHandler`, `ChainContext`, `IChainStep` (CQRS)
  - [ ] `[Obsolete]` na `RegisterChain()` w CQRS
  - [ ] Usunięcie `src/SolTechnology.Core.Flow/` (cały katalog)

- [ ] **Documentation**
  - [ ] `docs/Story-Framework.md` - kompletny przewodnik użytkownika
  - [ ] `docs/Migration-To-Story.md` - przewodnik migracji
  - [ ] `CLAUDE.md` - aktualizacja Architecture Patterns
  - [ ] XML comments na wszystkich publicznych API
  - [ ] README update

- [ ] **Performance & Cleanup**
  - [ ] Performance benchmarks (vs ChainHandler)
  - [ ] Code review + cleanup
  - [ ] CI/CD pipelines (GitHub Actions + Azure DevOps)

## 🧪 Strategia Testowania

### Testy Jednostkowe (>90% coverage)
- StoryHandlerTests - podstawowa funkcjonalność
- ChapterTests - wykonanie rozdziałów
- InteractiveChapterTests - rozdziały z user input
- StoryEngineTests - orkiestracja i flow control
- ErrorHandlingTests - Result, AggregateError, stop-on-error
- InMemoryRepositoryTests - CRUD, thread-safety
- SqliteRepositoryTests - persistence, serialization
- StoryManagerTests - high-level orchestration

### Testy Integracyjne
- CalculateBestPath migration (same results as Chain)
- SampleOrderWorkflow migration (pause/resume)
- SaveCityStory end-to-end (Testcontainers SQL)
- REST API endpoints

### Performance Benchmarks
- 5-step story vs old ChainHandler (<5% overhead)
- Story with persistence (InMemory vs SQLite)
- Large story (50+ chapters)

## 📚 Dokumentacja

### docs/Story-Framework.md
- Wprowadzenie (Tale Code philosophy)
- Quick Start (5-minute example)
- Core Concepts (StoryHandler, Narration, Chapter)
- Automated vs Interactive Chapters
- Error Handling
- Persistence (InMemory vs SQLite)
- REST API
- Best Practices & Anti-patterns

### docs/Migration-To-Story.md
- Overview (why migrate)
- Migration from ChainHandler (step-by-step)
- Migration from PausableChainHandler (step-by-step)
- Search & Replace Guide
- Common Issues & FAQ
- Breaking Changes
- Deprecation Timeline

### CLAUDE.md Update
- Replace Chain/Flow sections with Story Pattern
- Usage examples
- Registration patterns

## ✅ Kryteria Akceptacji

### Funkcjonalne
- [ ] Prosty 3-chapter story działa bez opcji
- [ ] Złożony 5+ chapter story działa
- [ ] Interactive chapter pauzuje i wznawia
- [ ] InMemory persistence zapisuje/wczytuje state
- [ ] SQLite persistence działa z bazy
- [ ] StoryManager pozwala start/resume
- [ ] Błędy agregowane w AggregateError
- [ ] Wszystkie 3 use cases zmigrowane i działają identycznie

### Niefunkcjonalne
- [ ] Performance: <5% overhead vs ChainHandler (bez persistence)
- [ ] Code coverage: >90%
- [ ] Dokumentacja kompletna
- [ ] Migration guide jasny i testowany
- [ ] Zero breaking changes dla istniejącego kodu (marked Obsolete)
- [ ] CI/CD pipelines przechodzą

## 📊 Postęp

### Week 1: Core Framework
- Status: 🟢 Completed
- Progress: 10/10 tasks

### Week 2: Persistence & Engine
- Status: 🟡 In Progress (Tests Completed)
- Progress: 7/8 tasks (remaining: StoryEngine persistence integration)

### Week 3: Advanced & Migration
- Status: 🔴 Not Started
- Progress: 0/7 tasks

### Week 4: API, Docs & Cleanup
- Status: 🔴 Not Started
- Progress: 0/6 tasks

**Overall Progress: 17/31 (55%)**

## 🚀 Następne Kroki

1. ✅ Plan zatwierdzony
2. ✅ Utworzenie projektu `SolTechnology.Core.Story`
3. ✅ Implementacja core abstractions
4. ✅ Podstawowe testy
5. ⏭️ Week 2: Models and Persistence implementation
6. ⏭️ Week 2: StoryEngine persistence integration
7. ⏭️ Week 2: Integration tests for persistence

---

**Last Updated:** 2025-12-24
**Updated By:** Claude Sonnet 4.5
