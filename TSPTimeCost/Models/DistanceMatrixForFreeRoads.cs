/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models
{
    public sealed class DistanceMatrixForFreeRoads : IDistanceMatrix
    {
        public IDistanceMatrix GetInstance()
        {
            if (_instance == null)
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new DistanceMatrixForFreeRoads();
                }
            }

            return _instance;
        }

        public double[] Value { get; set; }
        private static volatile DistanceMatrixForFreeRoads _instance;
        private static readonly object SyncRoot = new object();

        private DistanceMatrixForFreeRoads() { }

        public static IDistanceMatrix Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new DistanceMatrixForFreeRoads();
                    }
                }

                return _instance;
            }
        }
    }
}