using System.Collections.Generic;

namespace DreamTravel.BestPath.Interfaces
{
    public interface IEvaluationMatrix
    {
        double[] FreeDistances { get; set; }
        double[] TollDistances { get; set; }
        List<double> OptimalDistances { get; set; }
        double[] Goals { get; set; }
        double[] Costs { get; set; }
        double[] OptimalCosts { get; set; }
        double[] VinietaCosts { get; set; }
    }
}
