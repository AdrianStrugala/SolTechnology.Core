namespace TravelingSalesmanProblem.Models
{
    public interface IEvaluationMatrix
    {
        double[] Distances { get; set; }
        double[] Goals { get; set; }
        double[] Costs { get; set; }
    }
}
