using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace SolTechnology.Core.CQRS.Decorators.Validation;

public class CommandHandlerValidationDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : IRequest, IRequest<Result>
{
    private readonly ICommandHandler<TCommand> _handler;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public CommandHandlerValidationDecorator(
        ICommandHandler<TCommand> handler,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _handler = handler;
        _validators = validators;
    }

    public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            errors.AddRange((await validator.ValidateAsync(command, cancellationToken)).Errors);
        }

        if (errors.Any())
        {
            var errorMessage = BuildErrorMessage(errors);
            return Result.Fail(errorMessage);
        }
        else
        {

            return await _handler.Handle(command, cancellationToken);
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