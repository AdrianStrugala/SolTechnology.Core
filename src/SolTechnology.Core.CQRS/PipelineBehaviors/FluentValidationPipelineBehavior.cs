using FluentValidation;
using SolTechnology.Core.CQRS.Errors;

namespace SolTechnology.Core.CQRS.PipelineBehaviors;

/// <summary>
/// Runs all registered <see cref="IValidator{T}"/> for the request. On failure, short-circuits
/// the pipeline with <see cref="Result.Fail(Error)"/> carrying a <see cref="ValidationError"/>
/// — never throws for <see cref="ICommand"/>/<see cref="IQuery{TResult}"/> paths.
/// </summary>
public sealed class FluentValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public FluentValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationResults = new List<FluentValidation.Results.ValidationResult>();
        foreach (var validator in _validators)
        {
            validationResults.Add(await validator.ValidateAsync(context, cancellationToken));
        }

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (errors.Count == 0)
        {
            return await next();
        }

        var grouped = errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var validationError = new ValidationError
        {
            Message = "Validation failed",
            Errors = grouped
        };

        return ValidationFailureFactory.Create<TResponse>(validationError);
    }
}
