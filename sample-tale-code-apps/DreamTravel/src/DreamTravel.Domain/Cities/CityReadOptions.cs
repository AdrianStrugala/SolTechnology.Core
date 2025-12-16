namespace DreamTravel.Domain.Cities;

public class CityReadOptions
{
    public StatisticsOptions? Statistics { get; private set; }
    
    // Future extensions:
    // public WeatherOptions? Weather { get; private set; }
    // public AttractionsOptions? Attractions { get; private set; }

    public static CityReadOptions Default => new();

    /// <summary>
    /// Include all statistics
    /// </summary>
    public CityReadOptions WithStatistics()
    {
        Statistics = new StatisticsOptions();
        return this;
    }

    /// <summary>
    /// Include statistics for specific date range
    /// </summary>
    public CityReadOptions WithStatistics(DateOnly from, DateOnly to)
    {
        Statistics = new StatisticsOptions
        {
            From = from,
            To = to
        };
        return this;
    }
}

public class StatisticsOptions
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
