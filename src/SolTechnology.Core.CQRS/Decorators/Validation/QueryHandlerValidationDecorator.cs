using FluentValidation;
using FluentValidation.Results;

namespace SolTechnology.Core.CQRS.Decorators.Validation;

public class QueryHandlerValidationDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
{
    private readonly IEnumerable<IValidator<TQuery>> _validators;
    private readonly IQueryHandler<TQuery, TResult> _handler;

    public QueryHandlerValidationDecorator(
        IQueryHandler<TQuery, TResult> handler,
        IEnumerable<IValidator<TQuery>> validators)
    {
        _handler = handler;
        _validators = validators;
    }

    public async Task<TResult> Handle(TQuery command)
    {
        var errors = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            errors.AddRange((await validator.ValidateAsync(command)).Errors);
        }

        if (errors.Any())
        {
            var errorMessage = BuildErrorMessage(errors);
            throw new ArgumentException(errorMessage);
        }
        else
        {

            return await _handler.Handle(command);
        }
    }

    //From FluentValidator ValidationException
    private static string BuildErrorMessage(IEnumerable<ValidationFailure> errors)
    {
        var arr = errors.Select(x =>
            $"{Environment.NewLine} -- {x.PropertyName}: {x.ErrorMessage} Severity: {x.Severity.ToString()}");
        return "Validation failed: " + string.Join(string.Empty, arr);
    }
}