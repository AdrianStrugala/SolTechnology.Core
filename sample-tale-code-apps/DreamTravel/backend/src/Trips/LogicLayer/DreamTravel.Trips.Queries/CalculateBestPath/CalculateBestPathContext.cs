namespace DreamTravel.Trips.Queries.CalculateBestPath;

public sealed class CalculateBestPathContext
{
    public double[] FreeDistances { get; set; }
    public double[] TollDistances { get; set; }
    public double[] OptimalDistances { get; set; }
    public double[] Goals { get; set; }
    public double[] Costs { get; set; }
    public double[] OptimalCosts { get; set; }
    public double[] VinietaCosts { get; set; }


    public CalculateBestPathContext(int noOfCities)
    {
        int matrixSize = noOfCities * noOfCities;

        FreeDistances = new double[matrixSize];
        TollDistances = new double[matrixSize];
        OptimalDistances = new double[matrixSize];
        Goals = new double[matrixSize];
        Costs = new double[matrixSize];
        OptimalCosts = new double[matrixSize];
        VinietaCosts = new double[matrixSize];
    }
}