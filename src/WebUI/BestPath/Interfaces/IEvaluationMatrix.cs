namespace DreamTravel.WebUI.BestPath.Interfaces
{
    public interface IEvaluationMatrix
    {
        double[] FreeDistances { get; set; }
        double[] TollDistances { get; set; }
        double[] OptimalDistances { get; set; }
        double[] Goals { get; set; }
        double[] Costs { get; set; }
        double[] OptimalCosts { get; set; }
        double[] VinietaCosts { get; set; }
    }
}
