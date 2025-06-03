# Flow Workflow System Documentation (Chain-Based)

This document provides an overview of the Flow Workflow system, its components, API, and how to define new workflows using the chain-based approach. This system is designed for stateful, pausable workflows.

## 1. Core Concepts and Architecture

The system is built around a chain-of-responsibility pattern, adapted for persistence and user interaction pauses.

-   **`ChainContext<TInput, TOutput>`**: The primary stateful entity for a workflow instance. It holds:
    -   `TInput Input`: The initial input for the workflow.
    -   `TOutput Output`: The eventual result of the workflow.
    -   `CurrentStepId`: Assembly-qualified name of the active or last executed step type.
    -   `Status`: Current `FlowStatus` of the workflow (e.g., Running, WaitingForInput, Failed, Completed).
    -   `History`: A list of `ExecutedStepInfo` detailing each executed step.
    -   `ErrorMessage`: Any error message if the flow failed.
-   **`IFlowStep<TContext>`**: The base interface for all workflow steps. Its `Execute(TContext context)` method returns a `Result`.
    -   **`InteractiveFlowStep<TContext, TStepInput>`**: Specializes `IFlowStep` for steps requiring user input. (Note: TStepOutput removed for alignment with current InteractiveFlowStep)
        -   `GetRequiredInputSchema()`: Provides metadata about the expected `TStepInput`.
        -   `ExecuteWithUserInput(TContext context, TStepInput userInput)`: Processes user-provided data.
        -   Its `Execute` method typically returns `Result.Paused(...)` if input is pending.
    -   **`AutomatedFlowStep<TContext>`**: A marker interface for steps that run without direct user interaction.
-   **`Result`**: A class indicating a step's outcome:
    -   `IsSuccess`: True if the step logic completed successfully for this execution.
    -   `IsPaused`: True if the workflow should pause (e.g., waiting for user input).
    -   `Error`: Error message if failed.
    -   `PausedReason`, `RequiredInputSchemaForPause`: Details for UI steps.
-   **`PausableChainHandler<TInput, TContext, TOutput>`**: Abstract class orchestrating step execution.
    -   Derived classes implement `HandleChainDefinition(TContext context)` to define the sequence of steps using `Invoke<TStep>()`.
    -   `ExecuteHandler(FlowInstance flowInstance, TContext context, string? requestedStepId, JsonElement? requestedStepInput, CancellationToken cancellationToken)`: Manages running/resuming the chain, reacting to `Result` from steps.
    -   `Invoke<TStep>`: Executes a single step, updates context history (setting `CurrentStepId` to the step's AQN), and handles step results.
-   **`FlowInstance`**: Represents a persisted instance of a workflow. Contains:
    -   `FlowId`: Unique identifier.
    -   `FlowHandlerName`: The assembly-qualified name of the concrete `PausableChainHandler` type.
    -   `Context`: The actual `TContext` object associated with the handler (persisted).
    -   `Status`: Denormalized status from the context.
-   **`FlowManager`**: Service for managing the lifecycle of `FlowInstance`s:
    -   `StartFlow`: Creates and starts a new flow instance.
    -   `RunFlow`: Resumes a paused flow instance, processing provided user input if applicable.
-   **`IFlowInstanceRepository`**: Interface for persisting `FlowInstance` data.
    -   `InMemoryFlowInstanceRepository`: Provided for testing.
    -   `SqliteFlowInstanceRepository`: A concrete implementation using SQLite to persist flow instances, serializing `Context` to JSON.
-   **`FlowController`**: ASP.NET Core API controller for interacting with flows.

## 2. API Endpoints

Base route: `/api/flow`

-   **`POST /api/flow/{flowHandlerName}/start`**
    -   **Purpose**: Starts a new instance of the workflow defined by `flowHandlerName`.
    -   **`flowHandlerName`**: A string key (e.g., "SampleOrderWorkflow") registered in the application and mapped to a specific `PausableChainHandler` type. This allows the controller to dynamically invoke the correct workflow.
    -   **Request Body**: `JsonElement` (representing the initial input object) - Initial data for the workflow's `TInput`.
    -   **Response**:
        -   If paused for UI input (e.g., first step is user interaction):
            ```json
            {
              "flowId": "some-guid",
              "currentStep": {
                "stepId": "Full.Namespace.And.StepTypeName, AssemblyName", // AssemblyQualifiedName
                "type": "Interactive", // Or "Backend"
                "requiredData": [ { "name": "PropertyName", "type": "TypeName", "isComplex": false, "children": [] } ]
              },
              "status": "WaitingForInput",
              "history": [ /* ...list of StepInfo... */ ]
              // Context is not directly returned, but included in the persisted FlowInstance
            }
            ```
        -   If completed or failed: General flow status, including output or error message within the `FlowInstance` structure.

-   **`POST /api/flow/{flowId}`**
    -   **Purpose**: Resumes a flow, typically providing input for a paused step.
    -   **`flowId`**: The ID of the flow to resume.
    -   **`[FromBody] JsonElement? userInputJson`**: User input for the current step.
    -   **`[FromQuery] string? stepId`**: Optional specific step ID to target for resumption (if different from current).
    -   **Response**: Similar to `/start`, reflecting the new state (paused again, completed, failed) within the `FlowInstance`.

-   **`GET /api/flow/{flowId}`**
    -   **Purpose**: Retrieves the current state of a flow.
    -   **Response**: The `FlowInstance` object.
-   **`GET /api/flow/{flowId}/result`**
    -   **Purpose**: Retrieves the `Output` part of the context of a completed flow.
    -   **Response**: The `TOutput` object of the flow.


## 3. Defining and Registering New Workflows (Flows)

1.  **Define `TInput`, `TOutput`, `TContext`**:
    -   Create classes for your workflow's specific input, output, and context (deriving from `FlowContext<TInput, TOutput>`).
2.  **Implement Steps**:
    -   Create classes implementing `AutomatedFlowStep<TContext>` or `InteractiveFlowStep<TContext, TStepInput>`.
    -   For user interaction steps, define the `TStepInput` DTO.
3.  **Create `PausableChainHandler` Derivative**:
    -   Inherit from `PausableChainHandler<TInput, TContext, TOutput>`.
    -   Implement `HandleChainDefinition(TContext context)` by calling `await Invoke<YourStepType>();` for each step in sequence.
4.  **Dependency Injection**:
    -   Register your repository implementation (e.g., `services.AddSingleton<IFlowInstanceRepository, InMemoryFlowInstanceRepository>();`).
    -   Register `FlowManager` (e.g., `services.AddScoped<FlowManager>();`).
    -   Register your concrete handler (e.g., `services.AddScoped<YourWorkflowHandler>();` or map its string name in `FlowController`).
    -   Register all your step classes (e.g., `services.AddTransient<YourStepType>();`).
    -   Register all `IFlowHandler` implementations by scanning assemblies or manually:
        ```csharp
        // Example: services.AddAllImplementations<IFlowHandler>(assemblies);
        // This is needed for the FlowController to discover available flow handlers.
        ```

## 4. Example Workflow: `SampleOrderWorkflowHandler`

-   **Context**: `SampleOrderContext` (Input: `SampleOrderInput`, Output: `SampleOrderResult`)
-   **Steps**:
    -   `RequestUserInputStep` (implements `InteractiveFlowStep` for customer details).
    -   `BackendProcessingStep` (automated).
    -   `FetchExternalDataStep` (automated).
-   Demonstrates the structure in `src/SolTechnology.Core.Flow/Workflow/Handlers/` and `Steps/`. (Path updated)

## 5. Persistence

-   `FlowInstance` (which contains the `TContext`) is persisted via `IFlowInstanceRepository`.
-   `InMemoryFlowInstanceRepository` is provided for basic testing.
-   `SqliteFlowInstanceRepository` provides durable storage using a local SQLite database. It stores the main `FlowInstance` properties in distinct columns and serializes the `Context` object to a JSON string in the `ContextDataJson` column. On retrieval, it uses the `FlowHandlerName` (which should be the AssemblyQualifiedName of the handler) to help resolve the concrete `TContext` type for deserialization.

## 6. Logging

-   Uses `Microsoft.Extensions.Logging.ILogger`.
-   Logging is present in `PausableChainHandler`, `FlowManager`, and `FlowController`.
