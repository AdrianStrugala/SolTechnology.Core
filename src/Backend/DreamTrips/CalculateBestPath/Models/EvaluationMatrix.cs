namespace DreamTravel.DreamTrips.CalculateBestPath.Models
{
    public sealed class EvaluationMatrix
    {
        public double[] FreeDistances { get; set; }
        public double[] TollDistances { get; set; }
        public double[] OptimalDistances { get; set; }
        public double[] Goals { get; set; }
        public double[] Costs { get; set; }
        public double[] OptimalCosts { get; set; }
        public double[] VinietaCosts { get; set; }


        public EvaluationMatrix(int noOfCities)
        {
            FreeDistances = new double[noOfCities * noOfCities];
            TollDistances = new double[noOfCities * noOfCities];
            OptimalDistances = new double[noOfCities * noOfCities];
            Goals = new double[noOfCities * noOfCities];
            Costs = new double[noOfCities * noOfCities];
            OptimalCosts = new double[noOfCities * noOfCities];
            VinietaCosts = new double[noOfCities * noOfCities];
        }
    }
}