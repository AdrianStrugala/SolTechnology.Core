using System;

/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCostConsole {
    public sealed class NoCostMatrix {

        public double[] value { get; set; }
        private static volatile NoCostMatrix instance;
        private static object syncRoot = new Object();

        private NoCostMatrix() {}

        public static NoCostMatrix Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new NoCostMatrix();
                    }
                }

                return instance;
            }
        }
    }
}