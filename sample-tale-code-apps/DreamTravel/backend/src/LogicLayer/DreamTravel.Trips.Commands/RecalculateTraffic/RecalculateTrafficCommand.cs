using DreamTravel.Trips.Domain.StreetGraph;
using MediatR;
using System.Collections.Generic;

namespace DreamTravel.Trips.Commands.RecalculateTraffic;

public record RecalculateTrafficCommand(
    List<Street> Streets,
    List<Intersection> Intersections) 
    : IRequest<RecalculateTrafficResult>;

public record RecalculateTrafficResult(
    bool Success, 
    string Message, 
    List<TrafficSegment> UpdatedSegments = null);

public record TrafficSegment(
    string StreetId,
    double TrafficRegularTime,
    double TrafficRegularSpeed);
