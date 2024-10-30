using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SolTechnology.Core.Logging;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TResponse : class
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    //MediatR pipeline behavior
    //Logs operation started and finished (with success or exception) and execution time

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string operationName;

        if (request is ILoggableOperation loggedOperation)
        {
            _logger.BeginOperationScope(new KeyValuePair<string, object>(
                loggedOperation.LogScope.OperationIdName,
                loggedOperation.LogScope.OperationId));

            operationName = loggedOperation.LogScope.OperationName;
        }
        else
        {
            operationName = typeof(TRequest).FullName;
        }

        var sw = Stopwatch.StartNew();
        _logger.OperationStarted(operationName);

        try
        {
            var result = await next();
            _logger.OperationSucceeded(operationName, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception e)
        {
            _logger.OperationFailed(operationName, sw.ElapsedMilliseconds, e);
            throw;
        }
    }
}