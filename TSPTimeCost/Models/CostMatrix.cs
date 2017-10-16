using System;

/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class CostMatrix {

        public double[] Value { get; set; }
        private static volatile CostMatrix _instance;
        private static object syncRoot = new Object();

        private CostMatrix() {}

        public static CostMatrix Instance {
            get {
                if (_instance == null) {
                    lock (syncRoot) {
                        if (_instance == null)
                            _instance = new CostMatrix();
                    }
                }

                return _instance;
            }
        }
    }
}