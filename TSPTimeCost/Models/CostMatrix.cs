/***************************************************/
/* Singleton containing distance matrix            */
/***************************************************/

namespace TSPTimeCost.Models {
    public sealed class CostMatrix {

        public double[] Value { get; set; }
        private static volatile CostMatrix _instance;
        private static readonly object SyncRoot = new object();

        private CostMatrix() {}

        public static CostMatrix Instance {
            get {
                if (_instance == null) {
                    lock (SyncRoot) {
                        if (_instance == null)
                            _instance = new CostMatrix();
                    }
                }

                return _instance;
            }
        }
    }
}