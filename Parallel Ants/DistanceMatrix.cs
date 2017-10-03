using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost {
    public sealed class DistanceMatrix {

        public double[] value { get; set; }
        private static volatile DistanceMatrix instance;
        private static object syncRoot = new Object();

        private DistanceMatrix() {}

        public static DistanceMatrix Instance {
            get {
                if (instance == null) {
                    lock (syncRoot) {
                        if (instance == null)
                            instance = new DistanceMatrix();
                    }
                }

                return instance;
            }
        }
    }
}