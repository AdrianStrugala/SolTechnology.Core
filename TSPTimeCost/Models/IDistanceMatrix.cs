using System;

namespace TSPTimeCost.Models
{
    public interface IDistanceMatrix
    {
        IDistanceMatrix GetInstance();
        double[] Value { get; set; }
    }
}
