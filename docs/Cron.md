### Overview

The SolTechnology.Core.Scheduler library provides minimum functionality needed for scheduled background tasks. It handles needed services registration and configuration. It is based on Hangfire.Cronos library. The service is using in-memory scheduler, so does not share the scheduling information between instances.

### Registration

For installing the library, reference **SolTechnology.Core.Scheduler** nuget package and invoke **AddScheduledJob<T>()** service collection extension method:

```csharp
services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "ScheduledJobs": [
        {
        "JobName": "SynchornizeCristianoRonaldoMatches",
        "CronExpression": "0 0 * * *"
        }
    ]
  }
```

In this case, JobName has to match registered service (T) name.


2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
builder.Services.AddScheduledJob<SynchornizeCristianoRonaldoMatches>(new ScheduledJobConfiguration("0 0 * * *")); //every day at midnight
```


### Usage

1) Derivate from ScheduledJob base class

```csharp
public class SynchornizeCristianoRonaldoMatches : ScheduledJob
```

2) Implement constructor and Execute() method

```csharp
        public SynchornizeCristianoRonaldoMatches(
            ISchedulerConfigurationProvider schedulerConfigurationProvider,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ScheduledJob> logger)
            : base(schedulerConfigurationProvider, serviceScopeFactory, logger)
        {
            var scope = serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SynchronizePlayerMatchesCommand>>();
            _handler = handler;
        }

        public override async Task Execute()
        {
            await _handler.Handle(new SynchronizePlayerMatchesCommand(44));
        }
```

3) The job will run based on provided Cron expression.