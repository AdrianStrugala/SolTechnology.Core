/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class DistanceMatrixForTollRoads {

        public double[] Value { get; set; }
        private static volatile DistanceMatrixForTollRoads _instance;
        private static readonly object SyncRoot = new object();

        private DistanceMatrixForTollRoads() {}

        public static DistanceMatrixForTollRoads Instance {
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