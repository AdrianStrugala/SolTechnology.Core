namespace DreamTravel.Models
{
    public class Session
    {
        public string Id { get; set; }
        public int NoOfCities { get; set; }
        public double[] FreeDistances { get; set; }
        public double[] TollDistances { get; set; }
        public double[] Costs { get; set; }
    }
}
