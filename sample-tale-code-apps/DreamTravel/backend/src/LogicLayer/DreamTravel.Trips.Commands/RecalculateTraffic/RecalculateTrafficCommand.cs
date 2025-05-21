using DreamTravel.Trips.Domain.StreetGraph;
using MediatR;
using System.Collections.Generic;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Trips.Commands.RecalculateTraffic;

public record RecalculateTrafficCommand(
    List<Street> Streets,
    List<Street> NewStreets,
    List<Intersection> Intersections) 
    : IRequest<Result<List<Street>>>;
