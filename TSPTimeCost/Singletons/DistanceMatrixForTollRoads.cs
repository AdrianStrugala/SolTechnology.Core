/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Singletons {
    public sealed class DistanceMatrixForTollRoads : IDistanceMatrix {
        public IDistanceMatrix GetInstance()
        {
            if (_instance == null)
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new DistanceMatrixForTollRoads();
                }
            }

            return _instance;
        }

        public double[] Value { get; set; }
        private static volatile DistanceMatrixForTollRoads _instance;
        private static readonly object SyncRoot = new object();

        private DistanceMatrixForTollRoads() {}

        public static IDistanceMatrix Instance {
            get {
                if (_instance == null) {
                    lock (SyncRoot) {
                        if (_instance == null)
                            _instance = new DistanceMatrixForTollRoads();
                    }
                }

                return _instance;
            }
        }
    }
}