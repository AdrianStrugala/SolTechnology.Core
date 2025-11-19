using DreamTravel.Trips.Sql.DbModels;

namespace DreamTravel.Trips.Commands.DomainServices.CityDomain.SaveSteps;

public interface IIncrementSearchCountStep
{
    public void Invoke(CityEntity cityEntity, DateOnly date);
}

public class IncrementSearchCountStep : IIncrementSearchCountStep
{
    /// <summary>
    /// 2) Zwiększ liczbę wyszukiwań dla miasta (CityId, Date).
    /// Jeśli nie ma rekordu, utwórz z SearchCount = 1.
    /// </summary>
    public void Invoke(CityEntity cityEntity, DateOnly date)
    {
        var statistics = cityEntity.CityStatistics
            .FirstOrDefault(s => s.CityId == cityEntity.Id && s.Date == date);

        if (statistics != null)
        {
            statistics.SearchCount++;
            return;
        }

        cityEntity.CityStatistics.Add(new CityStatisticsEntity
        {
            CityId = cityEntity.Id,
            Date = date,
            SearchCount = 1
        });
    }
}