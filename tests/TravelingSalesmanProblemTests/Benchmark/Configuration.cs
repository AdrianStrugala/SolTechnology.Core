namespace TravelingSalesmanProblemTests.Benchmark
{
    class Configuration
    {

        public God God { get; set; }
        public AntColony AntColony { get; set; }
    }

}

class God
{
    public bool TwoCities { get; set; }
    public bool FourCities { get; set; }
    public bool EightCities { get; set; }
    public bool SixteenCities { get; set; }
    public bool TwelveCities { get; set; }
    public bool TwentyCities { get; set; }
}

class AntColony
{
    public bool TwoCities { get; set; }
    public bool FourCities { get; set; }
    public bool EightCities { get; set; }
    public bool SixteenCities { get; set; }
    public bool TwelveCities { get; set; }
    public bool TwentyCities { get; set; }
}
