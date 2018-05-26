namespace TravelingSalesmanProblem.Models
{
    public interface IDistanceMatrix
    {
        double[] Distances { get; set; }
        double[] Goals { get; set; }
        double[] Costs { get; set; }
    }
}
