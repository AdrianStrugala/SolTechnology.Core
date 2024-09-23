using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;

namespace DreamTravel.Infrastructure.Events;

public class HangfireNotificationPublisher : INotificationPublisher
{
    public Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            // Enqueue each handler as a Hangfire job
            BackgroundJob.Enqueue(() => ExecuteHandler(handler, notification));
        }

        return Task.CompletedTask;
    }
  
    
    private static Task ExecuteHandler(NotificationHandlerExecutor handler, INotification notification)
    {
        // Execute the handler
        return handler.HandlerCallback(notification, CancellationToken.None);
    }
}