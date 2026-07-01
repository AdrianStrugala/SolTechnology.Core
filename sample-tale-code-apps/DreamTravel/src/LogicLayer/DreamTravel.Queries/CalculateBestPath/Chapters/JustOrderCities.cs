using JetBrains.Annotations;
using SolTechnology.Core;
using SolTechnology.Core.Tale;

namespace DreamTravel.Queries.CalculateBestPath.Chapters;

/// <summary>
/// Fallback chapter for <see cref="FindProfitablePath"/>. When the toll-vs-free economics cannot be
/// resolved, drop the cost optimisation entirely and route purely by driving distance: the free
/// distances become the optimal distances and every toll cost is zeroed. <c>SolveTsp</c> then orders
/// the cities on that plain-distance matrix.
/// </summary>
[UsedImplicitly]
public class JustOrderCities : Chapter<CalculateBestPathContext>
{
    public override Task<Result> Read(CalculateBestPathContext context)
    {
        for (int i = 0; i < context.OptimalDistances.Length; i++)
        {
            context.OptimalDistances[i] = context.FreeDistances[i];
            context.OptimalCosts[i] = 0;
        }

        return Result.SuccessAsTask();
    }
}

