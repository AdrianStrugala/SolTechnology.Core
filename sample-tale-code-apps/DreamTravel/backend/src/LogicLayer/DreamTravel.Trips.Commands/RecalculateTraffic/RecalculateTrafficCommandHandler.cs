// using DreamTravel.Trips.Domain.StreetGraph;
// using MediatR;
// using Microsoft.Extensions.Logging;
// using SolTechnology.Core.CQRS;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace DreamTravel.Trips.Commands.RecalculateTraffic;
//
// public class RecalculateTrafficCommandHandler(ILogger<RecalculateTrafficCommandHandler> logger)
//     : ICommandHandler<RecalculateTrafficCommand, List<Street>>
// {
//     public async Task<Result<List<Street>>> Handle(RecalculateTrafficCommand request, CancellationToken cancellationToken)
//     {
//             logger.LogInformation("Starting traffic recalculation with {StreetCount} streets and {IntersectionCount} intersections", 
//                 request.Streets.Count, request.Intersections.Count);
//        
//             var newStreets = request.NewStreets;
//             
//             logger.LogInformation("Found {NewStreetCount} new streets to process", newStreets.Count);
//
//             // Build graph with all streets (existing and new)
//             var graph = BuildGraph(request.Streets, request.Intersections);
//             
//             // Calculate the before-and-after traffic metrics
//             var beforeMetrics = CalculateNetworkMetrics(request.Streets);
//             
//             // After the recalculation
//             var updatedSegments = CalculateTrafficFlowWithMaxFlow(request.Streets, newStreets, request.Intersections);
//             
//             var afterMetrics = CalculateNetworkMetrics(request.Streets
//                 .Select(s => 
//                 {
//                     var updatedSegment = updatedSegments.FirstOrDefault(us => us.StreetId == s.Id);
//                     if (updatedSegment != null)
//                     {
//                         // Create a copy with updated TrafficRegularSpeed
//                         var updatedStreet = s with { };
//                         updatedStreet.TrafficRegularSpeed = updatedSegment.TrafficRegularSpeed;
//                         return updatedStreet;
//                     }
//                     return s;
//                 })
//                 .ToList());
//             
//             logger.LogInformation("Traffic recalculation completed. Average speed before: {BeforeSpeed}, after: {AfterSpeed}", 
//                 beforeMetrics.AverageSpeed, afterMetrics.AverageSpeed);
//             
//             return updatedSegments;
//     }
//
//     private Dictionary<string, List<(string DestinationId, Street Street)>> BuildGraph(List<Street> streets, List<Intersection> intersections)
//     {
//         var graph = new Dictionary<string, List<(string DestinationId, Street Street)>>();
//         
//         // Initialize graph with all intersections
//         foreach (var intersection in intersections)
//         {
//             graph[intersection.Id] = new List<(string DestinationId, Street Street)>();
//         }
//         
//         // Add all streets as edges
//         foreach (var street in streets)
//         {
//             if (!graph.ContainsKey(street.FromId) || !graph.ContainsKey(street.ToId))
//             {
//                 // Skip streets with unknown intersections
//                 continue;
//             }
//             
//             // Add bidirectional edges for two-way streets and single edge for one-way streets
//             graph[street.FromId].Add((street.ToId, street));
//             
//             // if (!street.Oneway)
//             // {
//                 graph[street.ToId].Add((street.FromId, street));
//             // }
//         }
//         
//         return graph;
//     }
//
//     private (double AverageSpeed, double TotalTravelTime) CalculateNetworkMetrics(List<Street> streets)
//     {
//         var totalLength = 0.0;
//         var totalTravelTime = 0.0;
//         var countedStreets = 0;
//         
//         foreach (var street in streets)
//         {
//             if (street.TrafficRegularSpeed.HasValue && street.TrafficRegularSpeed > 0)
//             {
//                 totalLength += street.Length ?? 0;
//                 totalTravelTime += ((street.Length ?? 0) / street.TrafficRegularSpeed.Value) * 60; // in minutes
//                 countedStreets++;
//             }
//         }
//         
//         if (countedStreets == 0)
//             return (0, 0);
//             
//         return (totalLength / totalTravelTime * 60, totalTravelTime); // Convert to km/h
//     }
//
//     private List<TrafficSegment> CalculateTrafficFlowWithMaxFlow(
//         List<Street> allStreets, 
//         List<Street> newStreets,
//         List<Intersection> intersections)
//     {
//         // Initialize traffic segments for all streets
//         var result = new List<TrafficSegment>();
//         
//         // First, give base speeds to all new streets
//         foreach (var street in newStreets)
//         {
//             var baseSpeed = CalculateBaseSpeed(street);
//             var trafficTime = street.Length / baseSpeed * 60; // minutes
//             result.Add(new TrafficSegment(street.Id, trafficTime!.Value, baseSpeed));
//         }
//         
//         // Create a residual graph (for max flow calculation)
//         var residualGraph = CreateResidualGraph(allStreets, intersections);
//         
//         // Calculate flow improvement for all affected streets using Ford-Fulkerson
//         // For simplicity, let's select a few key "source" and "sink" nodes for flow calculation
//         
//         // Find central/important intersections (those with most connections)
//         var intersectionRanking = intersections
//             .Select(i => (
//                 Intersection: i, 
//                 Connections: allStreets.Count(s => s.FromId == i.Id || s.ToId == i.Id)
//             ))
//             .OrderByDescending(x => x.Connections)
//             .ToList();
//         
//         // Take top 20% as sources and bottom 20% as sinks
//         var sourceCandidates = intersectionRanking
//             .Take(Math.Max(1, intersectionRanking.Count / 5))
//             .Select(x => x.Intersection.Id)
//             .ToList();
//             
//         var sinkCandidates = intersectionRanking
//             .Skip(intersectionRanking.Count - Math.Max(1, intersectionRanking.Count / 5))
//             .Select(x => x.Intersection.Id)
//             .ToList();
//         
//         // For all source-sink pairs, calculate flow before and after
//         var trafficFlowFactors = new Dictionary<string, double>(); // street id -> traffic factor
//         
//         foreach (var sourceId in sourceCandidates)
//         {
//             foreach (var sinkId in sinkCandidates)
//             {
//                 if (sourceId == sinkId)
//                     continue;
//                 
//                 // Calculate maximum flow for the network without new streets
//                 var flowsBefore = CalculateMaxFlow(residualGraph, sourceId, sinkId, newStreets.Select(s => s.Id).ToHashSet());
//                 
//                 // Calculate maximum flow for the network with all streets
//                 var flowsAfter = CalculateMaxFlow(residualGraph, sourceId, sinkId, new HashSet<string>());
//                 
//                 // For each street that had flow in either scenario, calculate the ratio
//                 foreach (var streetId in flowsAfter.Keys.Union(flowsBefore.Keys))
//                 {
//                     var before = flowsBefore.GetValueOrDefault(streetId, 0);
//                     var after = flowsAfter.GetValueOrDefault(streetId, 0);
//                     
//                     // If the street didn't exist before or had no flow, use 1.0 as baseline
//                     if (before == 0) before = 1;
//                     
//                     // Calculate a factor based on how much flow improved
//                     var factor = Math.Max(1.0, after / before);
//                     
//                     // Limit improvement to realistic levels
//                     factor = Math.Min(1.5, factor);
//                     
//                     // Accumulate this factor
//                     if (trafficFlowFactors.ContainsKey(streetId))
//                     {
//                         trafficFlowFactors[streetId] = (trafficFlowFactors[streetId] + factor) / 2;
//                     }
//                     else
//                     {
//                         trafficFlowFactors[streetId] = factor;
//                     }
//                 }
//             }
//         }
//         
//         // Apply the calculated factors to all streets not yet in the result list
//         foreach (var street in allStreets.Where(s => s.TrafficRegularSpeed.HasValue && s.TrafficRegularSpeed > 0))
//         {
//             if (result.Any(r => r.SegmentId == street.Id))
//                 continue;
//                 
//             var factor = trafficFlowFactors.GetValueOrDefault(street.Id, 1.0);
//             
//             // Apply the factor - this is the improvement due to the new streets
//             var originalSpeed = street.TrafficRegularSpeed.Value;
//             var newSpeed = originalSpeed * factor;
//             
//             // Calculate new traffic time
//             var newTime = street.Length / newSpeed * 60; // minutes
//             
//             result.Add(new TrafficSegment(street.Id, newTime.GetValueOrDefault(), newSpeed));
//         }
//         
//         // Return the updated traffic segments
//         return result;
//     }
//     
//     private Dictionary<string, Dictionary<string, double>> CreateResidualGraph(List<Street> streets, List<Intersection> intersections)
//     {
//         // Create a graph with edge capacities (for max flow algorithm)
//         var residualGraph = new Dictionary<string, Dictionary<string, double>>();
//         
//         // Initialize with all intersections
//         foreach (var intersection in intersections)
//         {
//             residualGraph[intersection.Id] = new Dictionary<string, double>();
//         }
//         
//         // Add all streets with capacities
//         foreach (var street in streets)
//         {
//             if (!residualGraph.ContainsKey(street.FromId) || !residualGraph.ContainsKey(street.ToId))
//                 continue;
//                 
//             // Capacity is proportional to the number of lanes and inversely proportional to length
//             var capacity = (street.Lanes * 100.0 / Math.Max(0.1, street.Length.GetValueOrDefault())).GetValueOrDefault();
//             
//             // Add bidirectional edges for two-way streets, single edge for one-way
//             residualGraph[street.FromId][street.ToId] = capacity;
//             
//             // if (!street.Oneway)
//             // {
//             residualGraph[street.ToId][street.FromId] = capacity;
//             // }
//         }
//         
//         return residualGraph;
//     }
//     
//     private Dictionary<string, double> CalculateMaxFlow(
//         Dictionary<string, Dictionary<string, double>> residualGraph, 
//         string sourceId, 
//         string sinkId,
//         HashSet<string> excludedStreetIds)
//     {
//         // Implementation of Ford-Fulkerson algorithm for maximum flow
//         var flow = new Dictionary<string, Dictionary<string, double>>();
//         var streetFlow = new Dictionary<string, double>();
//         
//         // Initialize flow as 0 for all edges
//         foreach (var fromId in residualGraph.Keys)
//         {
//             flow[fromId] = new Dictionary<string, double>();
//             foreach (var toId in residualGraph[fromId].Keys)
//             {
//                 flow[fromId][toId] = 0;
//             }
//         }
//         
//         // Map to track which street corresponds to which edge
//         var edgeToStreet = new Dictionary<(string From, string To), Street>();
//         
//         // Use BFS to find augmenting paths
//         bool FindAugmentingPath(out List<string> path, out double bottleneck)
//         {
//             path = new List<string>();
//             bottleneck = double.MaxValue;
//             
//             var visited = new HashSet<string>();
//             var queue = new Queue<(string Node, List<string> Path, double Flow)>();
//             queue.Enqueue((sourceId, new List<string> { sourceId }, double.MaxValue));
//             
//             while (queue.Count > 0)
//             {
//                 var (node, currentPath, minFlow) = queue.Dequeue();
//                 
//                 if (node == sinkId)
//                 {
//                     path = currentPath;
//                     bottleneck = minFlow;
//                     return true;
//                 }
//                 
//                 if (visited.Contains(node))
//                     continue;
//                     
//                 visited.Add(node);
//                 
//                 if (!residualGraph.ContainsKey(node))
//                     continue;
//                     
//                 foreach (var neighbor in residualGraph[node].Keys)
//                 {
//                     var capacity = residualGraph[node][neighbor];
//                     var currentFlow = flow[node].GetValueOrDefault(neighbor, 0);
//                     var residualCapacity = capacity - currentFlow;
//                     
//                     // Skip edges with no remaining capacity or excluded streets
//                     if (residualCapacity <= 0 || (edgeToStreet.TryGetValue((node, neighbor), out var street) && excludedStreetIds.Contains(street.Id)))
//                         continue;
//                         
//                     var newPath = new List<string>(currentPath) { neighbor };
//                     queue.Enqueue((neighbor, newPath, Math.Min(minFlow, residualCapacity)));
//                 }
//             }
//             
//             return false;
//         }
//         
//         // Find augmenting paths and update flow until no more paths exist
//         while (FindAugmentingPath(out var path, out var bottleneck))
//         {
//             // Update flow along the path
//             for (int i = 0; i < path.Count - 1; i++)
//             {
//                 var from = path[i];
//                 var to = path[i + 1];
//                 
//                 // Increase forward flow
//                 if (!flow[from].ContainsKey(to))
//                     flow[from][to] = 0;
//                     
//                 flow[from][to] += bottleneck;
//                 
//                 // Update flow tracking for corresponding street
//                 // Since we don't have a direct mapping here, we'll need to look it up
//                 if (edgeToStreet.TryGetValue((from, to), out var street))
//                 {
//                     if (!streetFlow.ContainsKey(street.Id))
//                         streetFlow[street.Id] = 0;
//                         
//                     streetFlow[street.Id] += bottleneck;
//                 }
//                 
//                 // Decrease reverse flow (if exists)
//                 if (flow.ContainsKey(to) && flow[to].ContainsKey(from))
//                 {
//                     flow[to][from] -= bottleneck;
//                 }
//             }
//         }
//         
//         return streetFlow;
//     }
//
//     private double CalculateBaseSpeed(Street street)
//     {
//         // Base speed calculation based on street type
//         return street.Highway switch
//         {
//             "motorway" => 110.0,
//             "trunk" => 90.0,
//             "primary" => 70.0,
//             "secondary" => 50.0,
//             "tertiary" => 40.0,
//             "residential" => 30.0,
//             "service" => 20.0,
//             _ => 30.0
//         };
//     }
// }
