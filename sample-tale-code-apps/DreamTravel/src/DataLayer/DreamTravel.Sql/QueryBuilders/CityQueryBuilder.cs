using DreamTravel.Domain.Cities;
using DreamTravel.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Sql.QueryBuilders;

public static class CityQueryBuilder
{
    private const double DefaultCoordinateTolerance = 0.001; // ~100m

    /// <summary>
    /// Apply includes based on read options
    /// </summary>
    public static IQueryable<CityEntity> ApplyReadOptions(
        this IQueryable<CityEntity> query,
        CityReadOptions? options)
    {
        if (options == null) return query;

        if (options.Statistics != null)
        {
            // Wyciągamy wartości do zmiennych lokalnych, aby EF mógł użyć ich jako parametrów SQL
            var fromDate = options.Statistics.From;
            var toDate = options.Statistics.To;

            query = query.Include(c => c.Statistics
                .Where(s =>
                    // Logika musi być bezpośrednio w wyrażeniu lambda
                    (!fromDate.HasValue || s.Date >= fromDate.Value) &&
                    (!toDate.HasValue || s.Date <= toDate.Value)
                ));
        }

        return query;
    }
    

    /// <summary>
    /// Filter cities by alternative name (case-insensitive)
    /// </summary>
    public static IQueryable<CityEntity> WhereName(
        this IQueryable<CityEntity> query,
        string name)
    {
        var lowerName = name.ToLower();

        return query.Where(c =>
            c.AlternativeNames.Any(an =>
                an.AlternativeName.ToLower() == lowerName));
    }

    /// <summary>
    /// Filter cities by coordinates with default tolerance (~100m)
    /// </summary>
    public static IQueryable<CityEntity> WhereCoordinates(
        this IQueryable<CityEntity> query,
        double latitude,
        double longitude)
    {
        return query.WhereCoordinates(latitude, longitude, DefaultCoordinateTolerance);
    }

    /// <summary>
    /// Filter cities by coordinates with custom tolerance
    /// </summary>
    public static IQueryable<CityEntity> WhereCoordinates(
        this IQueryable<CityEntity> query,
        double latitude,
        double longitude,
        double tolerance)
    {
        return query.Where(c =>
            Math.Abs(c.Latitude - latitude) < tolerance &&
            Math.Abs(c.Longitude - longitude) < tolerance);
    }
}