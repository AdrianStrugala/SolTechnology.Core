# Journey Workflow System Documentation (Chain-Based)

This document provides an overview of the Journey Workflow system, its components, API, and how to define new workflows using the chain-based approach. This system is designed for stateful, pausable workflows.

## 1. Core Concepts and Architecture

The system is built around a chain-of-responsibility pattern, adapted for persistence and user interaction pauses.

-   **`ChainContext<TInput, TOutput>`**: The primary stateful entity for a workflow instance. It holds:
    -   `TInput Input`: The initial input for the workflow.
    -   `TOutput Output`: The eventual result of the workflow.
    -   `CurrentStepId`: Assembly-qualified name of the active or last executed step type.
    -   `Status`: Current `FlowStatus` of the workflow (e.g., Running, WaitingForInput, Failed, Completed).
    -   `History`: A list of `ExecutedStepInfo` detailing each executed step.
    -   `ErrorMessage`: Any error message if the flow failed.
-   **`IChainStep<TContext>`**: The base interface for all workflow steps. Its `Execute(TContext context)` method returns a `Result`.
    -   **`IUserInteractionChainStep<TContext, TStepInput, TStepOutput>`**: Specializes `IChainStep` for steps requiring user input.
        -   `GetRequiredInputSchema()`: Provides metadata about the expected `TStepInput`.
        -   `HandleUserInputAsync(TContext context, TStepInput userInput)`: Processes user-provided data.
        -   Its `Execute` method typically returns `Result.Paused(...)` if input is pending.
    -   **`IAutomatedChainStep<TContext>`**: A marker interface for steps that run without direct user interaction.
-   **`Result`**: A class indicating a step's outcome:
    -   `IsSuccess`: True if the step logic completed successfully for this execution.
    -   `IsPaused`: True if the workflow should pause (e.g., waiting for user input).
    -   `Error`: Error message if failed.
    -   `PausedReason`, `RequiredInputSchemaForPause`: Details for UI steps.
-   **`PausableChainHandler<TInput, TContext, TOutput>`**: Abstract class orchestrating step execution.
    -   Derived classes implement `HandleChainDefinition(TContext context)` to define the sequence of steps using `InvokeNextAsync<TStep>(context)`.
    -   `ExecuteHandler(TContext context, string? requestedStepId)`: Manages running/resuming the chain, reacting to `Result` from steps.
    -   `InvokeNextAsync`: Executes a single step, updates context history (setting `CurrentStepId` to the step's AQN), and handles step results.
-   **`JourneyInstance`**: Represents a persisted instance of a workflow. Contains:
    -   `JourneyId`: Unique identifier.
    -   `FlowHandlerName`: The assembly-qualified name of the concrete `PausableChainHandler` type.
    -   `ContextData`: The actual `TContext` object associated with the handler (persisted).
    -   `CurrentStatus`: Denormalized status from the context.
-   **`JourneyManager`**: Service for managing the lifecycle of `JourneyInstance`s:
    -   `StartJourneyAsync`: Creates and starts a new journey instance.
    -   `ResumeJourneyAsync`: Resumes a paused journey instance, processing provided user input if applicable.
-   **`IJourneyInstanceRepository`**: Interface for persisting `JourneyInstance` data.
    -   `InMemoryJourneyInstanceRepository`: Provided for testing.
    -   `SqliteJourneyInstanceRepository`: A concrete implementation using SQLite to persist journey instances, serializing `ContextData` to JSON.
-   **`JourneysController`**: ASP.NET Core API controller for interacting with journeys.

## 2. API Endpoints

Base route: `/api/journeys`

-   **`POST /api/journeys/{journeyHandlerName}/start`**
    -   **Purpose**: Starts a new instance of the workflow defined by `journeyHandlerName`.
    -   **`journeyHandlerName`**: A string key (e.g., "SampleOrderWorkflow") registered in the application and mapped to a specific `PausableChainHandler` type. This allows the controller to dynamically invoke the correct workflow.
    -   **Request Body**: `Dictionary<string, object>` (or a JSON object) - Initial data for the workflow's `TInput`.
    -   **Response**:
        -   If paused for UI input (e.g., first step is user interaction):
            ```json
            {
              "journeyId": "some-guid",
              "currentStep": {
                "stepId": "Full.Namespace.And.StepTypeName, AssemblyName", // AssemblyQualifiedName
                "type": "UI", 
                "requiredDataSchema": { "PropertyName": "TypeName", ... } 
              },
              "status": "WaitingForInput",
              "message": "Reason for pause"
            }
            ```
        -   If completed or failed: General journey status, including output or error message.

-   **`POST /api/journeys/{journeyId}/resume`**
    -   **Purpose**: Resumes a journey, typically providing input for a paused step.
    -   **Request Body**: `Dictionary<string, object>` (or a JSON object, optional) - User input for the current step. The system will attempt to deserialize this input to the type expected by the current interactive step.
    -   **Response**: Similar to `/start`, reflecting the new state (paused again, completed, failed).

-   **`GET /api/journeys/{journeyId}`**
    -   **Purpose**: Retrieves the current state of a journey.
    -   **Response**: Similar to `/start`, reflecting the current state.

## 3. Defining and Registering New Workflows (Journeys)

1.  **Define `TInput`, `TOutput`, `TContext`**:
    -   Create classes for your workflow's specific input, output, and context (deriving from `ChainContext<TInput, TOutput>`).
2.  **Implement Steps**:
    -   Create classes implementing `IAutomatedChainStep<TContext>` or `IUserInteractionChainStep<TContext, TStepInput, TStepOutput>`.
    -   For user interaction steps, define `TStepInput` and `TStepOutput` DTOs.
3.  **Create `PausableChainHandler` Derivative**:
    -   Inherit from `PausableChainHandler<TInput, TContext, TOutput>`.
    -   Implement `HandleChainDefinition(TContext context)` by calling `await InvokeNextAsync<YourStepType>(context);` for each step in sequence.
4.  **Dependency Injection**:
    -   Register your repository implementation (e.g., `services.AddSingleton<IJourneyInstanceRepository, SqliteJourneyInstanceRepository>();`).
    -   Register `JourneyManager` (e.g., `services.AddScoped<JourneyManager>();`).
    -   Register your concrete handler (e.g., `services.AddScoped<YourWorkflowHandler>();` or map its string name in `JourneysController`).
    -   Register all your step classes (e.g., `services.AddTransient<YourStepType>();`).

## 4. Example Workflow: `SampleOrderWorkflowHandler`

-   **Context**: `SampleOrderContext` (Input: `SampleOrderInput`, Output: `SampleOrderResult`)
-   **Steps**:
    -   `RequestUserInputStep` (implements `IUserInteractionChainStep` for customer details).
    -   `BackendProcessingStep` (automated).
    -   `FetchExternalDataStep` (automated).
-   Demonstrates the structure in `src/SolTechnology.Core.Journey/Workflow/Handlers/` and `Steps/`.

## 5. Persistence

-   `JourneyInstance` (which contains the `TContext`) is persisted via `IJourneyInstanceRepository`.
-   `InMemoryJourneyInstanceRepository` is provided for basic testing.
-   `SqliteJourneyInstanceRepository` provides durable storage using a local SQLite database. It stores the main `JourneyInstance` properties in distinct columns and serializes the `ContextData` object to a JSON string in the `ContextDataJson` column. On retrieval, it uses the `FlowHandlerName` (which should be the AssemblyQualifiedName of the handler) to help resolve the concrete `TContext` type for deserialization.

## 6. Logging

-   Uses `Microsoft.Extensions.Logging.ILogger`.
-   Logging is present in `PausableChainHandler`, `JourneyManager`, and `JourneysController`.
