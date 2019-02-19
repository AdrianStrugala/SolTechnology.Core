namespace DreamTravel.TravelingSalesmanProblem
{
    using System.Collections.Generic;

    public interface ITSP
    {
        List<int> SolveTSP(List<double> distances);
    }
}