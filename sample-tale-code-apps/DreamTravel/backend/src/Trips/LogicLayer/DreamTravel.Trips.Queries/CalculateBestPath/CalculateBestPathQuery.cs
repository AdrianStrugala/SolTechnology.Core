using DreamTravel.Trips.Domain.Cities;
using FluentValidation;

namespace DreamTravel.Trips.Queries.CalculateBestPath;

public class CalculateBestPathQuery
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