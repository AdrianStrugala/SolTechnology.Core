using FluentValidation;
using MediatR;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.CalculateBestPath;

public class CalculateBestPathQuery : IRequest<Result<CalculateBestPathResult>>
{
    public List<CityQueryModel> Cities { get; set; } = new();

    public class CityQueryModel
    {
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = null!;
    }
}

public class CalculateBestPathQueryValidator : AbstractValidator<CalculateBestPathQuery>
{
    public CalculateBestPathQueryValidator()
    {
        RuleFor(x => x.Cities)
            .NotNull();
    }
}
