/***************************************************/
/* Singleton containing best path as order of cities */
/***************************************************/

namespace TESWebUI.Models {
    public sealed class BestPath {

        public int[] Order { get; set; }
        public double[] DistancesInOrder { get; set; }
        public double Distance { get; set; }
        public double Cost { get; set; }
        public string TimeOfExecution { get; set; }
        public double[] Goal { get; set; }
    }
}