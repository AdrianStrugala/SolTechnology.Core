using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/***************************************************/
/* Singleton containing best path as order of cities */
/***************************************************/

namespace TSPTimeCost {
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