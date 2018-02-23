/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Singletons
{
    public sealed class DistanceMatrixEvaluated : IDistanceMatrix
    {
        public IDistanceMatrix GetInstance()
        {
            if (_instance == null)
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new DistanceMatrixEvaluated();
                }
            }

            return _instance;
        }

        public double[] Distances { get; set; }
        public double[] Goals { get; set; }
        private static volatile DistanceMatrixEvaluated _instance;
        private static readonly object SyncRoot = new object();

        private DistanceMatrixEvaluated() { }

        public static IDistanceMatrix Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new DistanceMatrixEvaluated();
                    }
                }

                return _instance;
            }
        }
    }
}