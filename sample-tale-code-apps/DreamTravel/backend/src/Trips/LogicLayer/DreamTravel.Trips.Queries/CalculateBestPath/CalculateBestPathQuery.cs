using DreamTravel.Trips.Domain.Cities;
using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathQuery : IRequest<Result<CalculateBestPathResult>>
{
    public List<City?> Cities { get; set; } = new();
}

public class CalculateBestPathQueryValidator : AbstractValidator<CalculateBestPathQuery>
{
    public CalculateBestPathQueryValidator()
    {
        RuleFor(x => x.Cities)
            .NotNull();
    }
}