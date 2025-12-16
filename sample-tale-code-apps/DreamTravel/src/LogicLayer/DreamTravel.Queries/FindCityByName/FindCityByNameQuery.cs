using System.Text.Json.Serialization;
using DreamTravel.Domain.Cities;
using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;
using SolTechnology.Core.Logging;

namespace DreamTravel.Queries.FindCityByName;

public class FindCityByNameQuery : ILoggableOperation, IRequest<Result<City>>
{
    public string Name { get; set; } = null!;

    [JsonIgnore]
    public LogScope LogScope => new()
    {
        OperationName = nameof(FindCityByNameQuery),
        OperationIdName = nameof(Name),
        OperationId = Name
    };

}

public class FindCityByNameQueryValidator : AbstractValidator<FindCityByNameQuery>
{
    public FindCityByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty();
    }
}