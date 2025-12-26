### Overview

The SolTechnology.Core.Flow library provides a workflow and chain framework with support for interactive flows. It enables building complex, multi-step workflows that can be paused and resumed, with state persistence using either in-memory or SQLite storage.

### Registration

For installing the library, reference **SolTechnology.Core.Flow** nuget package and invoke **AddFlowFramework()** service collection extension method:

```csharp
services.AddFlowFramework();
```

### Configuration

By default, the library uses SQLite for persistence. You can customize the repository implementation:

```csharp
// Use in-memory storage (for testing)
services.AddScoped<IFlowInstanceRepository, InMemoryFlowInstanceRepository>();

// Or use SQLite (default)
services.AddScoped<IFlowInstanceRepository, SqliteFlowInstanceRepository>();
```

### Usage

1) Define Flow Context

```csharp
public class MyFlowContext : FlowContext<MyInput, MyOutput>
{
    public string CurrentStepData { get; set; }
}
```

2) Define Flow Steps

Automated steps run without user interaction:

```csharp
public class ProcessDataStep : AutomatedFlowStep<MyFlowContext>
{
    public override async Task Execute(MyFlowContext context)
    {
        // Automated processing logic
        context.CurrentStepData = "processed";
    }
}
```

Interactive steps pause and wait for user input:

```csharp
public class AwaitUserApprovalStep : InteractiveFlowStep<MyFlowContext>
{
    public override async Task Execute(MyFlowContext context)
    {
        // Logic before pausing
        context.CurrentStepData = "awaiting approval";

        // Flow pauses here, waiting for resume
    }

    public override async Task Resume(MyFlowContext context)
    {
        // Logic after user provides input
        // Access user input from context
    }
}
```

3) Define Flow Handler

```csharp
public class MyFlowHandler : PausableChainHandler<MyInput, MyFlowContext, MyOutput>
{
    protected override async Task HandleChain()
    {
        await Invoke<ProcessDataStep>();
        await Invoke<AwaitUserApprovalStep>(); // Flow pauses here
        await Invoke<FinalizeStep>();
    }
}
```

4) Start a Flow

```csharp
var flowManager = serviceProvider.GetRequiredService<FlowManager>();
var input = new MyInput { Data = "initial data" };

var flowInstance = await flowManager.StartFlow<MyFlowHandler>(input);
```

5) Resume a Paused Flow

```csharp
// Get the flow instance ID from the initial start
var flowInstanceId = flowInstance.Id;

// Provide user input
var userInput = new Dictionary<string, object>
{
    { "approved", true },
    { "comment", "Looks good!" }
};

await flowManager.ResumeFlow(flowInstanceId, userInput);
```

6) Check Flow Status

```csharp
var status = await flowManager.GetFlowStatus(flowInstanceId);

switch (status)
{
    case FlowStatus.Running:
        // Flow is currently executing
        break;
    case FlowStatus.Paused:
        // Flow is paused, waiting for input
        break;
    case FlowStatus.Completed:
        // Flow has finished successfully
        break;
    case FlowStatus.Failed:
        // Flow encountered an error
        break;
}
```

7) Use Flow Controller (Optional)

The library includes a FlowController for HTTP API integration:

```csharp
// Start flow via HTTP POST
POST /api/flow/start
{
    "flowType": "MyFlowHandler",
    "input": { "data": "initial data" }
}

// Resume flow via HTTP POST
POST /api/flow/resume/{flowInstanceId}
{
    "approved": true,
    "comment": "Looks good!"
}

// Get flow status via HTTP GET
GET /api/flow/status/{flowInstanceId}
```
