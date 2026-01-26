# SolTechnology.Core.Jobs

Hangfire utilities: smart retry with exponential backoff, correlation ID propagation, and structured error handling via `IJob` pattern.

## Installation

```bash
dotnet add package SolTechnology.Core.Jobs
```

## Quick Start

```csharp
// Register Hangfire utilities
services.AddHangfire();
```

This adds:
- Smart retry filter with exponential backoff
- Correlation ID propagation through background jobs

## Features

### 1. IJob Pattern

Structured way to write Hangfire jobs with Result pattern:

```csharp
public class SendEmailJob : IJob<SendEmailRequest>
{
    private readonly IEmailService _emailService;

    public SendEmailJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<JobError>> ExecuteAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _emailService.SendAsync(request.To, request.Subject, request.Body);

        if (result.IsFailure)
        {
            return JobError.Retryable("SendEmail", result.Error.Message);
        }

        return Result<JobError>.Success();
    }
}
```

#### Enqueue the job

```csharp
public class OrderController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;

    public OrderController(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    [HttpPost]
    public IActionResult CreateOrder(CreateOrderRequest request)
    {
        // Process order...

        // Enqueue email job
        var emailRequest = new SendEmailRequest
        {
            To = request.Email,
            Subject = "Order Confirmation",
            Body = $"Your order {orderId} has been placed."
        };

        _jobClient.Enqueue<SendEmailJob>(job => job.ExecuteAsync(emailRequest, default));

        return Ok();
    }
}
```

#### Job without input

```csharp
public class CleanupJob : IJob
{
    public async Task<Result<JobError>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Cleanup logic
        return Result<JobError>.Success();
    }
}

// Enqueue
_jobClient.Enqueue<CleanupJob>(job => job.ExecuteAsync(default));

// Or as recurring job
RecurringJob.AddOrUpdate<CleanupJob>("cleanup", job => job.ExecuteAsync(default), Cron.Daily);
```

### 2. Smart Retry with Exponential Backoff

The `HangfireSmartRetryAttribute` provides intelligent retry logic:
- Configurable delay progression
- Respects error recoverability (retryable vs non-retryable)
- Logs retry attempts with context

#### Default Retry Schedule

| Attempt | Delay |
|---------|-------|
| 1 | 10 seconds |
| 2 | 1 minute |
| 3 | 5 minutes |
| 4 | 30 minutes |
| 5 | 1 hour |
| 6 | 2 hours |
| 7 | 4 hours |
| 8 | 8 hours |
| 9 | 16 hours |
| 10 | 24 hours |

#### Custom delays

```csharp
services.AddHangfire(options =>
{
    options.RetryDelaysInSeconds = [5, 30, 120, 600, 3600];
});
```

### 3. JobError - Structured Error Handling

```csharp
// Retryable error - will be retried
return JobError.Retryable("SendEmail", "SMTP timeout");

// Non-retryable error - fails permanently
return JobError.NonRetryable("SendEmail", $"Invalid email: {email}");

// Convert from CQRS Error
return JobError.FromError(result.Error, "SendEmail");
```

#### Signal smart retry via exception

```csharp
public async Task<Result<JobError>> ExecuteAsync(OrderRequest request, CancellationToken ct)
{
    var result = await _orderService.ProcessAsync(request);

    if (result.IsFailure)
    {
        var jobError = JobError.FromError(result.Error, "ProcessOrder");

        // Smart retry filter will check jobError.Recoverable
        throw new JobExecutionException(jobError);
    }

    return Result<JobError>.Success();
}
```

### 4. Correlation ID Propagation

Maintains correlation IDs across background jobs:

```csharp
// Set before enqueuing (e.g., in middleware)
HangfireCorrelationIdFilter.SetCurrentCorrelationId(httpContext.TraceIdentifier);

// Enqueue - correlation ID is captured automatically
_jobClient.Enqueue<ProcessOrderJob>(job => job.ExecuteAsync(orderId, default));

// In job - retrieve correlation ID
var correlationId = HangfireCorrelationIdFilter.GetCurrentCorrelationId();
_logger.LogInformation("Processing with CorrelationId: [{CorrelationId}]", correlationId);
```

### 5. Event Publisher (Optional)

Fire-and-forget MediatR notifications via Hangfire:

```csharp
// Register
services.AddHangfireEventPublisher();

// Use
public class OrderService
{
    private readonly IHangfireEventPublisher _eventPublisher;

    public void CompleteOrder(Order order)
    {
        // ... complete order logic

        // Publish event asynchronously via Hangfire
        _eventPublisher.Publish(new OrderCompletedNotification(order.Id));
    }
}
```

## Configuration

```csharp
services.AddHangfire(options =>
{
    options.EnableSmartRetry = true;
    options.RetryDelaysInSeconds = [10, 60, 300, 1800, 3600];
    options.EnableCorrelationIdPropagation = true;
});
```

### Selective registration

```csharp
services
    .AddHangfireSmartRetry([30, 300, 3600])
    .AddHangfireCorrelationId()
    .AddHangfireEventPublisher();
```

## Dependencies

- `Hangfire.Core` (1.8.22+)
- `MediatR` (12.3.0+)
- `SolTechnology.Core.CQRS` (for Result pattern)
