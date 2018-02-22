using System;

namespace TSPTimeCost.Singletons
{
    public interface IDistanceMatrix
    {
        IDistanceMatrix GetInstance();
        double[] Value { get; set; }
    }
}
