using DreamTravel.Domain.StreetGraph;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.GetIntersections;

public class GetIntersectionsQuery : IQuery<List<Intersection>>;
