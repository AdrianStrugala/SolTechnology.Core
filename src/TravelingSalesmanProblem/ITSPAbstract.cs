using System.Collections.Generic;

namespace TravelingSalesmanProblem
{
    public interface ITSP
    {
        List<int> SolveTSP(List<double> distances);
    }
}