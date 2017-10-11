using System;

/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class DistanceMatrixForTollRoads {

        public double[] Value { get; set; }
        private static volatile DistanceMatrixForTollRoads _instance;
        private static object syncRoot = new Object();

        private DistanceMatrixForTollRoads() {}

        public static DistanceMatrixForTollRoads Instance {
            get {
                if (_instance == null) {
                    lock (syncRoot) {
                        if (_instance == null)
                            _instance = new DistanceMatrixForTollRoads();
                    }
                }

                return _instance;
            }
        }
    }
}