### Overview

The SolTechnology.Core.Logging library provides set of Ilogger extensions working well with Application Insights.

### Registration

For installing the library, reference **SolTechnology.Core.Logging** nuget package.

### Configuration

No extra configuration is needed.

### Usage

```csharp
builder.Services.AddLogging(c =>
        c.AddConsole()
        .AddApplicationInsights());
```

appsettings:
```csharp
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Debug": {
      "LogLevel": {
        "Default": "Trace",
        "Microsoft.Hosting": "Trace",
        "Microsoft": "Trace",
        "Microsoft.Hosting.Lifetime": "Trace"
      }
    },
    "ApplicationInsights": {
      "sampling": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 100
      },
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Information",
        "Microsoft": "Information",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
 ```


#### Operation scope:

1) Begin operation scope (creates a custom dimension - key useful for App Insights tracking)
```csharp
   using (_logger.BeginOperationScope(new KeyValuePair<string, object>(command.LogScope.OperationIdName, command.LogScope.OperationId)))
```

2) Log operation (action, method, execution) started status
```csharp
  _logger.OperationStarted(command.LogScope.OperationName);
```

3) Log operation succeeded or failed status
```csharp
   _logger.OperationSucceeded(command.LogScope.OperationName);
   _logger.OperationFailed(command.LogScope.OperationName, e);
```

#### Middleware:

1) Reference middleware
```csharp
   app.UseMiddleware<LoggingMiddleware>();
```

2) Expand log scope with whatever data is helpful from request body, path or query
```csharp
    _logger.AddToScope("userId", userId.ToString() ?? "unknown"),
```
 


### Usage Example result

| Timestamp  | Message  | CustomDimensions.PlayerId  |
|-----------------------------|------------------------------------------------------------|-----|
| 4/11/2022, 12:00:10.738 AM  | Operation: [CalculatePlayerStatistics]. Status: [START]    | 44  |
| 4/11/2022, 12:00:12.859 AM  | Operation: [CalculatePlayerStatistics]. Status: [SUCCESS]  | 44  |
