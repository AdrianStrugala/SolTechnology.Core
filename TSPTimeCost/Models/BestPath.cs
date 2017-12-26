/***************************************************/
/* Singleton containing best path as order of cities */
/***************************************************/

using System.Collections.Generic;
using System.Dynamic;

namespace TSPTimeCost.Models {
    public sealed class BestPath {

        public int[] Order { get; set; }
        public double[] DistancesInOrder { get; set; }
        public double Distance { get; set; }
        public double Cost { get; set; }
        public double[] Goal { get; set; }

        private static volatile BestPath _instance;
        private static readonly object SyncRoot = new object();

        private BestPath() { }

        public static BestPath Instance {
            get {
                if (_instance == null) {
                    lock (SyncRoot) {
                        if (_instance == null)
                            _instance = new BestPath();
                    }
                }

                return _instance;
            }
        }
    }
}