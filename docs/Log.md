# SolTechnology.Core.Logging

ILogger extensions and request logging middleware with correlation ID support and custom identifier extraction.

## Installation

```bash
dotnet add package SolTechnology.Core.Logging
```

## Features

### 1. LoggingMiddleware

Comprehensive request logging middleware that provides:
- Correlation ID extraction/generation and propagation
- Automatic identifier extraction from routes and query parameters
- Request timing and status code logging

#### Basic Usage

```csharp
app.UseLoggingMiddleware();
```

This will:
- Extract correlation ID from `X-Correlation-ID` header (or generate one)
- Add correlation ID to response headers
- Log request start/end with timing
- Make correlation ID available via `LoggingMiddleware.Current`

#### Custom Identifiers

```csharp
app.UseLoggingMiddleware(options =>
{
    options.Identifiers = ["trip", "city", "user"];
});
```

For each identifier, the middleware automatically extracts values from:
1. **Route path**: `/{identifier}/{value}` or `/{identifier}s/{value}`
   - `/trip/123` → `trip = 123`
   - `/trips/abc` → `trip = abc`
   - `/city/Warsaw` → `city = Warsaw`
   - `/cities/Krakow` → `city = Krakow`
2. **Query parameter**: `?{identifier}={value}`
   - `?trip=123` → `trip = 123`
   - `?city=Warsaw` → `city = Warsaw`

#### Custom Correlation ID Header

```csharp
app.UseLoggingMiddleware(options =>
{
    options.CorrelationIdHeader = "X-Request-Id";
    options.Identifiers = ["trip", "city"];
});
```

#### Accessing Correlation ID

```csharp
// Get current correlation ID anywhere in the request pipeline
var correlationId = LoggingMiddleware.Current;
```

### 2. Operation Scope Extensions

#### Begin operation scope (creates a custom dimension for App Insights tracking)

```csharp
using (_logger.BeginOperationScope(new KeyValuePair<string, object>("OrderId", orderId)))
{
    // All logs within this scope will include OrderId
}

// Or with an anonymous object
using (_logger.BeginOperationScope(new { OrderId = orderId, CustomerId = customerId }))
{
    // All logs within this scope will include both identifiers
}
```

#### Log operation lifecycle

```csharp
_logger.OperationStarted("ProcessOrder");

try
{
    // ... process order
    _logger.OperationSucceeded("ProcessOrder", stopwatch.ElapsedMilliseconds);
}
catch (Exception e)
{
    _logger.OperationFailed("ProcessOrder", stopwatch.ElapsedMilliseconds, e);
    throw;
}
```

### 3. Add to Scope Extension

```csharp
using (_logger.AddToScope("userId", userId))
{
    // All logs within this scope will include userId
}
```

## Configuration

### Serilog Integration

Include identifiers in the output template:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {trip} {city} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Example Output

| Timestamp | CorrelationId | trip | city | Message |
|-----------|---------------|------|------|---------|
| 12:00:10 | abc123 | 550e8400 | Warsaw | Started request: [GET /trips/550e8400...] |
| 12:00:12 | abc123 | 550e8400 | Warsaw | Finished request in [2100] ms with status code [200] |
