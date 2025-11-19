using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Sql.QueryBuilders;

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
        if(options == null) return query;
        
        if (options.Statistics != null)
        {
            query = query.Include(c => c.Statistics
                .Where(s => ApplyStatisticsFilter(s, options.Statistics)));
        }

        return query;
    }

    private static bool ApplyStatisticsFilter(CityStatisticsEntity s, StatisticsOptions options)
    {
        if (options.From.HasValue && s.Date < options.From.Value)
            return false;

        if (options.To.HasValue && s.Date > options.To.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Filter cities by alternative name (case-insensitive)
    /// </summary>
    public static IQueryable<CityEntity> WhereName(
        this IQueryable<CityEntity> query,
        string name,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return query.Where(c =>
            c.AlternativeNames.Any(an =>
                an.AlternativeName.Equals(name, comparison)));
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