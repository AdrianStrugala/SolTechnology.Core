namespace TravelingSalesmanProblem.Models {
    public sealed class Path
    {
        public City StartingCity { get; set; }
        public City EndingCity { get; set; }
        public double Distance { get; set; }
        public double Cost { get; set; }
        public double Goal { get; set; }
    }
}