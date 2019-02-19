namespace DreamTravel.WebUI.SharedModels
{
    public sealed class Path
    {
        public int Index { get; set; }
        public City StartingCity { get; set; }
        public City EndingCity { get; set; }
        public double OptimalDistance { get; set; }
        public double OptimalCost { get; set; }
        public double VinietaCost { get; set; }
        public double Goal { get; set; }
        public double Cost { get; set; }
        public double FreeDistance { get; set; }
        public double TollDistance { get; set; }

        public Path()
        {
            StartingCity = new City();
            EndingCity = new City();
        }
    }
}