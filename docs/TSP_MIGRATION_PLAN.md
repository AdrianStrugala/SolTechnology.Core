# Plan Migracji Angular DreamTravel do Blazor

## Analiza Obecnej Aplikacji Angular

### Struktura i Funkcjonalności
Aplikacja Angular zawiera kilka głównych modułów:

1. **Dream Trips** (TSP Solver) - główny feature do migracji
   - Mapa Google Maps z markerami
   - Panel boczny z listą miast
   - Dodawanie punktów przez kliknięcie na mapie lub wpisanie nazwy
   - Obliczanie optymalnej trasy TSP
   - Wyświetlanie wyników (czas podróży, koszty)
   - Integracja z płatnościami AIIA

2. **Dream Flights** - subskrypcje lotów
   - Formularz wyszukiwania lotów
   - Zarządzanie subskrypcjami email
   - Lista zapisanych subskrypcji

3. **Autentykacja/Konto użytkownika**
   - Login/Register
   - Zmiana hasła
   - LocalStorage persistence

### Technologie Angular
- Angular 16.2.0 + Material Design
- Google Maps: `@angular/google-maps`
- Reactive Forms z walidacją
- RxJS dla state management
- HTTP Client z custom auth header

### Obecny Backend API (v2)
Kluczowe endpointy:
- `POST /api/v2/FindCityByName` - wyszukiwanie miasta po nazwie
- `POST /api/v2/FindCityByCoordinates` - wyszukiwanie miasta po współrzędnych
- `POST /api/v2/CalculateBestPath` - obliczanie TSP
- `GET /api/v2/statistics/countries` - statystyki wyszukiwań

**UWAGA**: API używa response envelope `Result<T>` z polami `success`, `data`, `errors`

### Istniejąca Aplikacja Blazor (DreamTravel.UI)

#### Struktura
- **Blazor WebAssembly** (.NET 10.0)
- **MudBlazor 8.5.1** jako framework UI
- **Routing**: Komponenty stronnicowe z `@page`
- **Layout**: `MainLayout.razor` z `MudDrawer` (sidebar) + `MudMainContent`
- **Nawigacja**: `NavMenu.razor` z `MudNavMenu` i `MudNavLink`
- **Google Maps**: Już zaimplementowane w `Map.razor` z JS interop (`mapInterop.js`)

#### Obecne Strony/Taby
1. **Home** (`/`) - strona powitalna
2. **Map** (`/map`) - Road Planner z grafiką ulic
3. **Flow** (`/flow`) - placeholder logowania

#### Wzorce Implementacyjne
- **State Management**: Lokalne w `@code` blocks
- **HTTP Client**: Scoped service z `X-API-KEY` header
- **Services**: `GraphService.cs` dla komunikacji z backend
- **Layout**: 2-kolumnowy grid (`MudGrid` + `MudItem`) z responsive breakpoints
- **JS Interop**: `mapInterop.js` dla Google Maps API

## Zakres Migracji (Potwierdzony)

### Co migrujemy:
✅ **Dream Trips (TSP Solver)** - kompletny feature z mapą i obliczaniem tras
✅ **Płatności AIIA** - integracja z bramką płatności dla tras

### Czego NIE migrujemy:
❌ Dream Flights (subskrypcje lotów)
❌ System autentykacji (login/register) - **UWAGA**: Sprawdzić czy płatności wymagają auth

### Decyzje Architektoniczne:
1. **Google Maps**: Stworzyć współdzielony komponent `GoogleMap.razor` używany przez:
   - Istniejący `Map.razor` (Road Planner)
   - Nowy `TspMap.razor` (TSP Solver)

2. **Statystyki**: Pełna implementacja - wyświetlanie i zarządzanie `SearchStatistics` w UI

## ⚠️ KRYTYCZNE USTALENIE - Brak Endpointu Płatności

**Problem**: Endpoint `/api/users/pay` używany przez Angular **NIE ISTNIEJE** w obecnym backendzie DreamTravel.Api.

**Analiza**:
- Angular frontend wywołuje: `POST /api/users/pay` z `{userId, amount, currency}`
- Oczekuje: `{authorizationUrl, paymentId}`
- Backend: Brak UserController, brak payment endpoints
- Możliwe scenariusze:
  1. Endpoint był planowany ale nigdy nie zaimplementowany
  2. Angular używa innego backendu (zewnętrznego)
  3. Funkcja nigdy nie była używana w produkcji

**Opcje Implementacji** (wymaga decyzji użytkownika):

### Opcja A: Pominąć Płatności (Rekomendowane dla MVP)
- Zaimplementować UI z przyciskiem "Pay with AIIA" (disabled lub hidden)
- Dodać TODO comment dla przyszłej implementacji
- Skupić się na core TSP functionality
- **Czas**: Oszczędza ~2 dni pracy

### Opcja B: Zaimplementować Endpoint Płatności w Backendzie
- Stworzyć `UserController` w DreamTravel.Api
- Zaimplementować integrację z AIIA API
- Dodać testy dla payment flow
- **Czas**: +3-4 dni dodatkowej pracy
- **Wymaga**: Dokumentacji AIIA API, credentials testowych

### Opcja C: Mock Payment Flow
- Symulować przekierowanie do fałszywej strony płatności
- Return URL pokazuje "Payment Successful" (dla demo)
- **Czas**: +0.5 dnia

**DECYZJA**: Opcja A - Pominąć płatności (MVP)

---

## Plan Implementacji - Finalna Wersja

### Podsumowanie Zadania

Migracja Angular Dream Trips (TSP Solver) do Blazor WebAssembly jako nowy tab w DreamTravel.UI:

**Zakres:**
- ✅ Mapa Google Maps z dodawaniem miast (kliknięcie lub nazwa)
- ✅ Panel boczny z listą miast
- ✅ Obliczanie optymalnej trasy TSP
- ✅ Wyświetlanie wyników (czas, koszty, drogi płatne/bezpłatne)
- ✅ Pełne statystyki wyszukiwań miast
- ✅ Współdzielony komponent GoogleMap.razor
- ❌ Płatności AIIA (pominięte - TODO na przyszłość)

**Estymacja czasu:** 6 dni (zamiast 8, bez płatności)

---

## Architektura Rozwiązania

### 1. Struktura Komponentów

```
TspMap.razor (strona główna - /tsp-map)
├── CitiesPanel.razor (lewy panel - lista miast)
└── GoogleMap.razor (prawy panel - mapa)
```

### 2. Warstwa Serwisów

**TspService.cs** - komunikacja z API:
- `FindCityByNameAsync(name)` → `POST /api/v2/FindCityByName`
- `FindCityByCoordinatesAsync(lat, lng)` → `POST /api/v2/FindCityByCoordinates`
- `CalculateBestPathAsync(cities)` → `POST /api/v2/CalculateBestPath`
- `FormatTimeFromSeconds(seconds)` → helper do formatowania HH:MM:SS

### 3. JavaScript Interop

**Rozszerzenie mapInterop.js** (wsparcie dla wielu instancji map):

```javascript
window.mapInterop = {
    _maps: {},  // Store multiple maps by ID

    // TSP-specific functions
    addTspMarker(mapId, lat, lng, label, color)
    drawDirectionsPath(mapId, startLat, startLng, endLat, endLng, options)
    clearTspMarkers(mapId)
    clearDirections(mapId)
    fitBoundsToMarkers(mapId)
    requestGeolocation(mapId)
}
```

### 4. Modele Danych

**CityEntry.cs**:
```csharp
public class CityEntry {
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public City? City { get; set; }  // z DreamTravel.Trips.Domain
}
```

---

## Implementacja Krok po Kroku

### Faza 1: Fundament (Dzień 1) ✅

**1.1. Model danych** ✅
- Utworzyć: `Models/CityEntry.cs`

**1.2. Serwis API** ✅
- Utworzyć: `Services/TspService.cs`
- Zmodyfikować: `DependencyInjection/ServiceCollectionExtensions.cs`
  ```csharp
  services.AddScoped<TspService>();
  ```

**1.3. Testowanie**
- Sprawdzić czy API endpoints działają (Postman/Swagger)
- Zweryfikować Result<T> envelope format

### Faza 2: Infrastruktura Mapy (Dzień 2)

**2.1. JavaScript Interop**
- Zmodyfikować: `wwwroot/mapInterop.js`
- Dodać funkcje:
  - `initMap(mapId, lat, lng, zoom)` - wsparcie dla mapId
  - `addTspMarker(mapId, lat, lng, label, color)` - marker z numerem
  - `drawDirectionsPath(mapId, ...)` - DirectionsRenderer z obsługą OVER_QUERY_LIMIT
  - `clearTspMarkers(mapId)`, `clearDirections(mapId)`
  - `fitBoundsToMarkers(mapId)`
  - `requestGeolocation(mapId)` - Promise-based

**2.2. Współdzielony komponent mapy**
- Utworzyć: `Components/Shared/GoogleMap.razor`

**2.3. Testowanie**
- Stworzyć prostą stronę testową z GoogleMap.razor
- Sprawdzić czy mapa się ładuje
- Przetestować geolocation
- Przetestować kliknięcie na mapie

### Faza 3: Panel Miast (Dzień 3)

**3.1. Komponent panelu**
- Utworzyć: `Components/TspMap/CitiesPanel.razor`

### Faza 4: Strona Główna TSP (Dzień 4)

**4.1. Główny komponent**
- Utworzyć: `Pages/TspMap.razor`

### Faza 5: Nawigacja i Statystyki (Dzień 5)

**5.1. Nawigacja**
- Zmodyfikować: `Layout/NavMenu.razor`

**5.2. Statystyki (tooltip na markerach)**
- Rozszerzyć `addTspMarker` w mapInterop.js

### Faza 6: Testowanie i Poprawki (Dzień 6)

**6.1. Checklist testów manualnych**

| Test Case | Expected | Status |
|-----------|----------|--------|
| Dodaj miasto po nazwie | Marker pojawia się | ⬜ |
| Dodaj miasto kliknięciem | Geocoding działa | ⬜ |
| Usuń miasto | Marker znika | ⬜ |
| Run TSP (2 miasta) | Ścieżka narysowana | ⬜ |
| Run TSP (5+ miast) | Optymalna kolejność | ⬜ |
| Geolocation granted | Mapa centruje się | ⬜ |
| Geolocation denied | Fallback do default | ⬜ |
| API error | Snackbar z błędem | ⬜ |
| Responsive (mobile) | Panel scrolluje | ⬜ |
| Hover na marker | Tooltip ze statystykami | ⬜ |
| Blue path | Droga bezpłatna | ⬜ |
| Black path | Droga płatna | ⬜ |

---

## Pliki do Utworzenia (8 nowych)

1. ✅ `Models/CityEntry.cs`
2. ✅ `Models/TspDtos.cs`
3. ✅ `Services/TspService.cs`
4. ✅ `Components/Shared/GoogleMap.razor`
5. ✅ `Components/TspMap/CitiesPanel.razor`
6. ✅ `Pages/TspMap.razor`
7. 🔄 `tests/Component/Trips/TspUiIntegrationTest.cs` (w trakcie)

## Pliki do Zmodyfikowania (3 istniejące)

1. ✅ `wwwroot/mapInterop.js` - dodać funkcje TSP
2. ✅ `Layout/NavMenu.razor` - dodać link do TSP
3. ✅ `DependencyInjection/ServiceCollectionExtensions.cs` - zarejestrować TspService

---

## Testowanie Integracyjne

### Infrastruktura (Component Tests)

**Framework**: NUnit + Playwright + WireMock + TestContainers

**Fixtures** (SetUpFixture pattern):
- **ApiFixture<Program>** - In-memory test server dla DreamTravel.Api (WebApplicationFactory)
- **SqlFixture** - Docker SQL Server z DACPAC deployment
- **WireMockFixture** - HTTP mocking na porcie 2137

### Wzorzec Testu TSP UI

**Lokalizacja**: `tests/Component/Trips/TspUiIntegrationTest.cs`

**Scope**: Test end-to-end całego flow TSP z UI Blazor

**Pattern**:
```csharp
[SetUp]
public void Setup()
{
    _apiClient = ComponentTestsFixture.ApiFixture.ServerClient;
    _wireMockFixture = ComponentTestsFixture.WireMockFixture;
    _playwright = await Playwright.CreateAsync();
    _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
}

[Test]
public async Task TspFlow_AddCitiesAndCalculate_DisplaysResults()
{
    // Arrange - Mock Google API responses
    _wireMockFixture.Fake<IGoogleApiClient>()
        .WithRequest(x => x.GetLocationOfCity, "Warsaw")
        .WithResponse(x => x.WithSuccess().WithBody(
            GoogleFakeApi.BuildGeocodingResponse(new City {
                Name = "Warsaw",
                Latitude = 52.2297,
                Longitude = 21.0122
            })
        ));

    _wireMockFixture.Fake<IGoogleApiClient>()
        .WithRequest(x => x.GetDistanceMatrix, ...)
        .WithResponse(x => x.WithSuccess().WithBody(...));

    // Act - Playwright UI automation
    var page = await _browser.NewPageAsync();
    await page.GotoAsync("http://localhost:5000/tsp-map");

    // Add first city by name
    await page.FillAsync("input[label='City Name']", "Warsaw");
    await page.Keyboard.PressAsync("Enter");

    // Add second city by map click (mock geocoding)
    await page.ClickAsync("#tsp-map", new() { Position = new() { X = 300, Y = 200 } });

    // Run TSP
    await page.ClickAsync("button:has-text('Run TSP')");
    await page.WaitForSelectorAsync("text=Total Time:");

    // Assert - Verify results displayed
    var totalTime = await page.TextContentAsync("text=Total time:");
    totalTime.Should().NotBeNullOrEmpty();

    var totalCost = await page.TextContentAsync("text=Total cost:");
    totalCost.Should().Contain("DKK");
}
```

**Google API Mocking** (wykorzystanie istniejącego GoogleFakeApi.cs):
- Geocoding: `BuildGeocodingResponse(city)` - konwersja nazwy/współrzędnych na City
- Distance Matrix: `BuildDistanceMatrixResponse(...)` - dystanse między miastami dla TSP

**Test Cases**:
1. Dodanie miast po nazwie (mocked geocoding)
2. Dodanie miast kliknięciem (mocked reverse geocoding)
3. Obliczenie TSP (mocked distance matrix)
4. Wyświetlenie rezultatów (total time, cost, colored paths)
5. Walidacja błędów (brak miast, API error)

### Konfiguracja

**appsettings.tests.json** (już skonfigurowane):
```json
{
  "GoogleApi": {
    "BaseUrl": "http://localhost:2137/google/",
    "ApiKey": "test-key"
  }
}
```

**Referencje**:
- Test pattern: `CalculateBestPathFeatureTest.cs`
- WireMock API: `GoogleFakeApi.cs`
- Fixture setup: `ComponentTestsFixture.cs`

---

## Decyzje Architektoniczne i Trade-offs

| Decyzja | Uzasadnienie |
|---------|--------------|
| **Shared GoogleMap component** | DRY - reużywalny przez Map.razor i TspMap.razor |
| **Service layer (TspService)** | Separacja API logic, łatwiejsze testowanie |
| **Child components** | CitiesPanel wydzielony dla czystości kodu (Tale Code) |
| **Component state** | Nie potrzebujemy global state - TSP jest izolowany |
| **DirectionsRenderer** | Dokładne trasy Google Maps vs custom polylines |
| **Hardcoded DKK multiplier (7.46)** | Matching Angular behavior (TODO: verify if still needed) |
| **Pominąć płatności** | Endpoint nie istnieje - focus na core functionality |

---

## TODO na Przyszłość (Po Migracji)

1. **Płatności AIIA**: Zaimplementować endpoint `/api/users/pay` w backendzie
2. **Date range statistics**: Dodać DateRangePicker dla filtrowania statystyk
3. **Refactor Map.razor**: Użyć GoogleMap.razor w istniejącym Road Planner
4. **Path details modal**: Szczegóły każdego segmentu trasy
5. **Export route**: GPX/KML format dla nawigacji GPS
6. **Autentykacja**: Integracja z user context (jeśli będzie)
7. **Unit tests**: TspService, CitiesPanel, TspMap
8. **E2E tests**: Playwright scenarios

---

## Estymacja Czasu

| Faza | Zadania | Czas |
|------|---------|------|
| 1 | Fundament (modele, serwisy) | 1 dzień |
| 2 | Infrastruktura mapy (JS interop, GoogleMap) | 1 dzień |
| 3 | Panel miast (CitiesPanel UI) | 1 dzień |
| 4 | Strona główna (TspMap integration) | 1.5 dnia |
| 5 | Nawigacja, statystyki | 0.5 dnia |
| 6 | Testowanie, poprawki | 1 dzień |
| **TOTAL** | | **6 dni** |

---

## Kryteria Sukcesu

✅ Wszystkie funkcje Angular TSP zreplikowane (oprócz płatności)
✅ Mapa działa z geolocation i click-to-add
✅ TSP calculation zwraca optymalne trasy
✅ Paths renderowane z prawidłowymi kolorami (blue/black)
✅ Statystyki wyświetlane w tooltips
✅ Responsive design (mobile/desktop)
✅ Error handling kompletny
✅ Kod zgodny z Tale Code standards
✅ Brak regresji w istniejącym Map.razor
