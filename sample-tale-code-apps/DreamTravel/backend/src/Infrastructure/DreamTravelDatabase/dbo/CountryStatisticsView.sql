CREATE VIEW [dbo].[CountryStatisticsView]
AS
SELECT
    c.Country,
    SUM(cs.SearchCount) AS TotalSearchCount
FROM CityStatistics cs
         JOIN City c ON c.Id = cs.CityId
GROUP BY c.Country;
GO