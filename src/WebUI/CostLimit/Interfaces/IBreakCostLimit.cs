namespace DreamTravel.WebUI.CostLimit.Interfaces
{
    using System.Collections.Generic;
    using Contract;

    public interface IBreakCostLimit
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
