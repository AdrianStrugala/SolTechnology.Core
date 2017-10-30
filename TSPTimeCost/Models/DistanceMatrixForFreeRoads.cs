/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class DistanceMatrixForFreeRoads {

        public double[] Value { get; set; }
        private static volatile DistanceMatrixForFreeRoads _instance;
        private static readonly object SyncRoot = new object();

        private DistanceMatrixForFreeRoads() {}

        public static DistanceMatrixForFreeRoads Instance {
            get {
                if (_instance == null) {
                    lock (SyncRoot) {
                        if (_instance == null)
                            _instance = new DistanceMatrixForFreeRoads();
                    }
                }

                return _instance;
            }
        }
    }
}