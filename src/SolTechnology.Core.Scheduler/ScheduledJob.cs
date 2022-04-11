using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Scheduler.Configuration;

namespace SolTechnology.Core.Scheduler;

public abstract class ScheduledJob : IHostedService, IDisposable
{
    private readonly ISchedulerConfigurationProvider _schedulerConfigurationProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ScheduledJob> _logger;
    private readonly Type _jobType;

    private System.Timers.Timer _timer;


    protected ScheduledJob(
        ISchedulerConfigurationProvider schedulerConfigurationProvider,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ScheduledJob> logger)
    {
        _schedulerConfigurationProvider = schedulerConfigurationProvider;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _jobType = GetType();
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await ScheduleJob(cancellationToken);
    }

    protected async Task ScheduleJob(CancellationToken cancellationToken)
    {
        ScheduledJobConfiguration scheduledJobConfiguration = _schedulerConfigurationProvider.ResolveScheduledJobConfig(_jobType.Name);

        var next = scheduledJobConfiguration.CronExpression.GetNextOccurrence(DateTimeOffset.Now, scheduledJobConfiguration.TimeZoneInfo);
        if (next.HasValue)
        {
            var delay = next.Value - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0)   // prevent non-positive values from being passed into Timer
            {
                await ScheduleJob(cancellationToken);

            }
            _logger.LogInformation($"Job [{_jobType.Name}] runs in [{delay.ToString()}]");

            _timer = new System.Timers.Timer(delay.TotalMilliseconds);
            _timer.Elapsed += async (sender, args) =>
            {
                _timer.Dispose();  // reset and dispose timer
                _timer = null;

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ExecuteJob(cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken);    // reschedule next
                }
            };
            _timer.Start();
        }
        await Task.CompletedTask;
    }

    protected virtual async Task ExecuteJob(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var job = (ScheduledJob)scope.ServiceProvider.GetRequiredService(_jobType);
            await job.Execute();
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    public abstract Task Execute();

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        await Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        _timer?.Dispose();
    }
}