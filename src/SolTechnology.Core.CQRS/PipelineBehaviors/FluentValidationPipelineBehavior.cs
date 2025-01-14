using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

public class FluentValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TResponse : class where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public FluentValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    //MediatR pipeline behavior
    //Executes all fluent validators for given request

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationResults = _validators
            .Select(validator => validator.Validate(context))
            .ToList();

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (errors.Any())
        {
            throw new ValidationException(BuildErrorMessage(errors));
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
            WriteIndented = true
        };

        return JsonSerializer.Serialize(new { errors = groupedErrors }, options);
    }
}