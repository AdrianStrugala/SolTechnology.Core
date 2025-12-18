using DreamTravel.Sql.DbModels;

namespace DreamTravel.DomainServices.CityDomain.SaveSteps;

/// <summary>
/// Step responsible for incrementing search count statistics for a city.
/// </summary>
public interface IIncrementSearchCountStep
{
    /// <summary>
    /// Increments the search count for a city on a specific date.
    /// </summary>
    /// <param name="cityEntity">The city entity to update statistics for.</param>
    /// <param name="date">The date of the search.</param>
    public void Invoke(CityEntity cityEntity, DateOnly date);
}

/// <summary>
/// Implements the logic for incrementing city search count statistics.
/// Creates a new statistics record if one doesn't exist for the given date.
/// </summary>
public class IncrementSearchCountStep : IIncrementSearchCountStep
{
    /// <summary>
    /// Increments the search count for the specified city and date.
    /// If no record exists for the given date, creates a new one with SearchCount = 1.
    /// </summary>
    /// <param name="cityEntity">The city entity to update statistics for.</param>
    /// <param name="date">The date of the search.</param>
    public void Invoke(CityEntity cityEntity, DateOnly date)
    {
        var statistics = cityEntity.Statistics
            .FirstOrDefault(s => s.City.Id == cityEntity.Id && s.Date == date);

        if (statistics != null)
        {
            statistics.SearchCount++;
            return;
        }

        cityEntity.Statistics.Add(new CityStatisticsEntity
        {
            Date = date,
            SearchCount = 1
        });
    }
}