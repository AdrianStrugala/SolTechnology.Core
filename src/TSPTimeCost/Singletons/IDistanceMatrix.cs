namespace TSPTimeCost.Singletons
{
    public interface IDistanceMatrix
    {
        IDistanceMatrix GetInstance();
        double[] Distances { get; set; }
        double[] Goals { get; set; }
    }
}
