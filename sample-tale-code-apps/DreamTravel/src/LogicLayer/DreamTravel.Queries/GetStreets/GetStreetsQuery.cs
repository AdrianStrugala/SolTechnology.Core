using DreamTravel.Domain.StreetGraph;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Queries.GetStreets;

public class GetStreetsQuery : IQuery<List<Street>>;
