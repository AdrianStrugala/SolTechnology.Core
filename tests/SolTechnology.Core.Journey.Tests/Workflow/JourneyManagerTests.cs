// tests/SolTechnology.Core.Journey.Tests/Workflow/JourneyManagerTests.cs
using NUnit.Framework;
using SolTechnology.Core.Journey.Workflow.ChainFramework;
using SolTechnology.Core.Journey.Workflow.ChainFramework.Persistence.Sqlite; // Add this
using SolTechnology.Core.Journey.Workflow.Handlers; 
using SolTechnology.Core.Journey.Workflow.Steps; 
using SolTechnology.Core.Journey.Workflow.Steps.Dtos; 
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; 
using Microsoft.Extensions.DependencyInjection; 
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;
using System.IO; // Add this


namespace SolTechnology.Core.Journey.Tests.Workflow
{
    [TestFixture]
    public class JourneyManagerTests
    {
        private ServiceProvider _serviceProvider;
        private JourneyManager _journeyManager;
        private IJourneyInstanceRepository _journeyRepository;
        private string _testDbPath; // To store path to the test DB

        [SetUp]
        public void Setup()
        {
            // Define a unique DB path for this test fixture run
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_journeys_{Guid.NewGuid()}.db");
            // Ensure no old file exists from a previous failed run (optional, but good practice)
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);


            var services = new ServiceCollection();

            // Register SQLite Repository with the unique path
            services.AddSingleton<IJourneyInstanceRepository>(sp => new SqliteJourneyInstanceRepository(_testDbPath));

            // Register JourneyManager
            services.AddScoped<JourneyManager>();

            // Register Example Handler and its Steps
            services.AddScoped<SampleOrderWorkflowHandler>();
            services.AddTransient<RequestUserInputStep>();
            // BackendProcessingStep constructor has default false: public BackendProcessingStep(bool forceFailForTest = false)
            services.AddTransient<BackendProcessingStep>(); 
            services.AddTransient<FetchExternalDataStep>();

            // Register Logging
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton<ILogger<PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>>, NullLogger<PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>>>();
            // Add logger for SqliteJourneyInstanceRepository if it had one (it doesn't currently)

            _serviceProvider = services.BuildServiceProvider();

            _journeyManager = _serviceProvider.GetRequiredService<JourneyManager>();
            _journeyRepository = _serviceProvider.GetRequiredService<IJourneyInstanceRepository>();

            // No need to clear repository manually like with InMemory, as each test run (or fixture) gets a fresh DB file.
        }

        [TearDown]
        public void Teardown()
        {
            // Dispose the service provider, which should dispose services like SqliteJourneyInstanceRepository
             _serviceProvider?.Dispose();


            // Attempt to delete the database file.
            // This requires that the connection to the DB is properly closed.
            // SqliteJourneyInstanceRepository implements IDisposable which closes the connection.
            // The ServiceProvider's Dispose() should call the Dispose() on singleton/scoped IDisposable services.
            // Adding a small delay or retry can sometimes help if file lock issues are intermittent.
            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not delete test database '{_testDbPath}'. It might still be in use. Error: {ex.Message}");
                // This can happen on some systems if the file is locked. Not failing the test for this.
            }
        }

        [Test]
        public async Task StartJourneyAsync_SampleOrderWorkflow_RunsAndPausesOnUserInputStep()
        {
            // Arrange
            var initialInput = new SampleOrderInput { OrderId = "TestOrder123", Quantity = 2 };

            // Act
            var journeyInstance = await _journeyManager.StartJourneyAsync<SampleOrderWorkflowHandler, SampleOrderInput, SampleOrderContext, SampleOrderResult>(initialInput);

            // Assert
            Assert.IsNotNull(journeyInstance);
            StringAssert.Contains(typeof(SampleOrderWorkflowHandler).Name, journeyInstance.FlowHandlerName);

            var context = journeyInstance.ContextData as SampleOrderContext;
            Assert.IsNotNull(context);

            Assert.AreEqual(FlowStatus.WaitingForInput, context.Status);
            Assert.AreEqual("RequestCustomerDetails", context.CurrentStepId); 
            Assert.IsTrue(context.History.Any(h => h.StepId == "RequestCustomerDetails" && h.Status == FlowStatus.WaitingForInput));

            var persistedInstance = await _journeyRepository.GetByIdAsync(journeyInstance.JourneyId);
            Assert.IsNotNull(persistedInstance);
            var persistedContext = persistedInstance.ContextData as SampleOrderContext;
            Assert.IsNotNull(persistedContext);
            Assert.AreEqual(FlowStatus.WaitingForInput, persistedContext.Status);
        }

        [Test]
        public async Task ResumeJourneyAsync_SampleOrderWorkflow_WithValidInput_CompletesStepAndRunsNext()
        {
            // Arrange: Start and get to paused state
            var initialInput = new SampleOrderInput { OrderId = "TestOrder456", Quantity = 1 };
            var journeyInstance = await _journeyManager.StartJourneyAsync<SampleOrderWorkflowHandler, SampleOrderInput, SampleOrderContext, SampleOrderResult>(initialInput);
            var journeyId = journeyInstance.JourneyId;

            var userInput = new CustomerDetailsInput { Name = "John Doe", Address = "123 Main St" };

            var currentContext = journeyInstance.ContextData as SampleOrderContext;
            Assert.IsNotNull(currentContext, "Initial context is null.");
            Assert.AreEqual("RequestCustomerDetails", currentContext.CurrentStepId, "Not paused at expected step.");

            var inputStep = _serviceProvider.GetRequiredService<RequestUserInputStep>();
            var inputResult = await inputStep.HandleUserInputAsync(currentContext, userInput);
            Assert.IsTrue(inputResult.IsSuccess, "HandleUserInputAsync failed.");
            
            currentContext.Status = FlowStatus.Running; 
            journeyInstance.ContextData = currentContext;
            journeyInstance.CurrentStatus = currentContext.Status;
            await _journeyRepository.SaveAsync(journeyInstance);


            // Act: Now resume the main handler logic
            journeyInstance = await _journeyManager.ResumeJourneyAsync<SampleOrderWorkflowHandler, SampleOrderInput, SampleOrderContext, SampleOrderResult>(journeyId, null , currentContext.CurrentStepId);


            // Assert
            Assert.IsNotNull(journeyInstance);
            var finalContext = journeyInstance.ContextData as SampleOrderContext;
            Assert.IsNotNull(finalContext);

            Assert.AreEqual(FlowStatus.Completed, finalContext.Status, $"Journey did not complete. Status: {finalContext.Status}, Error: {finalContext.ErrorMessage}, History: {string.Join(", ", finalContext.History.Select(h => h.StepId + ":" + h.Status))}");
            Assert.IsTrue(finalContext.Output.IsSuccessfullyProcessed);
            StringAssert.Contains("Shipping estimate for Order TestOrder456: 2 days", finalContext.Output.FinalMessage);
            Assert.AreEqual($"Name: {userInput.Name}, Address: {userInput.Address}", finalContext.CustomerDetails);
        }

        [Test]
        public async Task StartJourneyAsync_SampleOrderWorkflow_BackendStepFails_JourneyStatusIsFailed()
        {
            // Arrange
            var initialInput = new SampleOrderInput { OrderId = "FailOrder789", Quantity = -1 }; 
            
            // This test uses its own ServiceProvider to inject a BackendProcessingStep configured to fail.
            var services = new ServiceCollection();
            // Use a different DB file for this specific test to ensure isolation if necessary, or rely on cleanup.
            // For this test, it will use the main _testDbPath if we use the main _journeyManager.
            // However, to inject a custom-configured step for a specific test,
            // it's often cleaner to create a specific handler or use a test-specific DI scope.
            // Here, we re-configure the service provider for this test as it was structured before.
            string testSpecificDbPath = Path.Combine(Path.GetTempPath(), $"test_journeys_fail_{Guid.NewGuid()}.db");
            if (File.Exists(testSpecificDbPath)) File.Delete(testSpecificDbPath);

            services.AddSingleton<IJourneyInstanceRepository>(sp => new SqliteJourneyInstanceRepository(testSpecificDbPath));
            services.AddScoped<JourneyManager>();
            services.AddScoped<SampleOrderWorkflowHandler>();
            services.AddTransient<RequestUserInputStep>();
            services.AddTransient<BackendProcessingStep>(sp => new BackendProcessingStep(true)); // true to force fail
            services.AddTransient<FetchExternalDataStep>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton<ILogger<PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>>, NullLogger<PausableChainHandler<SampleOrderInput, SampleOrderContext, SampleOrderResult>>>();
            var testServiceProvider = services.BuildServiceProvider();
            
            var journeyManagerForTest = testServiceProvider.GetRequiredService<JourneyManager>();
            var journeyRepoForTest = testServiceProvider.GetRequiredService<IJourneyInstanceRepository>();


            // Act:
            var journey = await journeyManagerForTest.StartJourneyAsync<SampleOrderWorkflowHandler, SampleOrderInput, SampleOrderContext, SampleOrderResult>(initialInput);
            var currentCtx = journey.ContextData as SampleOrderContext;
            Assert.AreEqual(FlowStatus.WaitingForInput, currentCtx.Status, "Journey should be waiting for user input first.");
            
            var inputStep = testServiceProvider.GetRequiredService<RequestUserInputStep>();
            var handleInputResult = await inputStep.HandleUserInputAsync(currentCtx, new CustomerDetailsInput { Name = "Test User", Address = "Test Address" });
            Assert.IsTrue(handleInputResult.IsSuccess, "HandleUserInputAsync for RequestUserInputStep failed.");
            currentCtx.Status = FlowStatus.Running; 
            journey.ContextData = currentCtx; 
            journey.CurrentStatus = currentCtx.Status;
            await journeyRepoForTest.SaveAsync(journey);

            journey = await journeyManagerForTest.ResumeJourneyAsync<SampleOrderWorkflowHandler, SampleOrderInput, SampleOrderContext, SampleOrderResult>(journey.JourneyId, null, currentCtx.CurrentStepId);
            var finalCtx = journey.ContextData as SampleOrderContext;

            // Assert
            Assert.IsNotNull(finalCtx);
            Assert.AreEqual(FlowStatus.Failed, finalCtx.Status, "Journey status should be Failed.");
            StringAssert.Contains("Invalid quantity for processing", finalCtx.ErrorMessage); // Updated to match new error message
            Assert.IsTrue(finalCtx.History.Any(h => h.StepId == "ProcessOrderPayment" && h.Status == FlowStatus.Failed));

            // Cleanup for this specific test's DB
            (testServiceProvider as IDisposable)?.Dispose();
             try
            {
                if (File.Exists(testSpecificDbPath))
                {
                    File.Delete(testSpecificDbPath);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not delete test-specific database '{testSpecificDbPath}'. Error: {ex.Message}");
            }
        }
    }
}
