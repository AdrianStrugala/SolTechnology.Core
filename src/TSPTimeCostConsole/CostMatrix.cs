using System;

/***************************************************/
/* Singleton containing cost matrix            */
/***************************************************/

namespace TSPTimeCostConsole {
    public sealed class CostMatrix {

        public double[] value { get; set; }
        private static volatile CostMatrix instance;
        private static object syncRoot = new Object();

        private CostMatrix() {}

        public static CostMatrix Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new CostMatrix();
                    }
                }

                return instance;
            }
        }
    }
}