namespace TSPTimeCost.Models
{
    public class Road
    {
        public string Beginning { get; set; }
        public string Ending { get; set; }
        public double Time { get; set; }
        public double Cost { get; set; }
        public double? TimeDecimal { get; set; }
    }
}
