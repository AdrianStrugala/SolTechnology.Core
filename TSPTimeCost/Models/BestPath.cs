using System;

/***************************************************/
/* Singleton containing best path as order of cities */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class BestPath {

        public int[] order { get; set; }
        public double distance { get; set; }
        private static volatile BestPath instance;
        private static object syncRoot = new Object();

        private BestPath() { }

        public static BestPath Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new BestPath();
                    }
                }

                return instance;
            }
        }
    }
}