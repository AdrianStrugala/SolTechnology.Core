using FluentValidation;
using SolTechnology.Core.CQRS;
using Path = DreamTravel.Domain.Paths.Path;

namespace DreamTravel.Queries.LimitCostOfPaths
{
    public class LimitCostOfPathsQuery : IQuery<List<Path>>
    {
        public int CostLimit { get; set; }
        public List<Path> Paths { get; set; } = null!;
    }

    public class LimitCostOfPathsQueryValidator : AbstractValidator<LimitCostOfPathsQuery>
    {
        public LimitCostOfPathsQueryValidator()
        {
            RuleFor(x => x.CostLimit)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Paths)
                .NotEmpty();
        }
    }
}
