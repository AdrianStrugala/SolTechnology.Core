using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

public class FluentValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<FluentValidationPipelineBehavior<TRequest, TResponse>> _logger;

    public FluentValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<FluentValidationPipelineBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    //MediatR pipeline behavior
    //Executes all fluent validators for given request

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationResults = _validators
            .Select(validator => validator.Validate(context))
            .ToList();

        var validationFailures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (validationFailures.Any())
        {
            var errorMessage = BuildErrorMessage(validationFailures);
            _logger.LogWarning(errorMessage);

            var responseType = typeof(TResponse);
            if (responseType == typeof(Result) || (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>)))
            {
                Type genericType = responseType.GetGenericArguments()[0];
                Type closedGenericType = typeof(Result<>).MakeGenericType(genericType);
                MethodInfo failMethod = closedGenericType.GetMethod(nameof(Result.Fail), BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Error) }, null);

                var error = new Error
                {
                    Message = "Validation failed",
                    Description = errorMessage
                };

                object instance = failMethod.Invoke(null, new object[] { error });


                return instance as TResponse;
            }
        }

        return await next();
    }

    private string BuildErrorMessage(IEnumerable<ValidationFailure> errors)
    {
        var groupedErrors = errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );


        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Ensures special characters are not escaped
        };

        return JsonSerializer.Serialize(new { errors = groupedErrors }, options);
    }
}