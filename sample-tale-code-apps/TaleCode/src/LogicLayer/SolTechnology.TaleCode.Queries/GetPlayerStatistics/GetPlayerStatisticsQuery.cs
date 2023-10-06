using FluentValidation;

namespace SolTechnology.TaleCode.PlayerRegistry.Queries.GetPlayerStatistics;

public class GetPlayerStatisticsQuery
{
    public int PlayerId { get; set; }

    public GetPlayerStatisticsQuery(int playerId)
    {
        PlayerId = playerId;
    }
}

public class GetPlayerStatisticsQueryValidator : AbstractValidator<GetPlayerStatisticsQuery>
{
    public GetPlayerStatisticsQueryValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotNull()
            .NotEmpty()
            .GreaterThan(0);
    }
}