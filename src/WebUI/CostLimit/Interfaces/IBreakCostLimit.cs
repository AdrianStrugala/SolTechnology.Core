namespace WebUI.CostLimit.Interfaces
{
    using System.Collections.Generic;
    using SharedModels;

    public interface IBreakCostLimit
    {
        List<Path> Execute(int costLimit, List<Path> paths);
    }
}
