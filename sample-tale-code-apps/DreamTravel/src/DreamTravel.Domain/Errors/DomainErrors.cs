using SolTechnology.Core.CQRS.Errors;

namespace DreamTravel.Domain.Errors;

/// <summary>
/// Domain-specific error definitions for DreamTravel.
/// </summary>
public static class DomainErrors
{
    public static class City
    {
        public static Error NotFound(string cityName) =>
            Error.NotFound(
                source: nameof(City),
                message: $"City [{cityName}] was not found",
                details: new Dictionary<string, object> { ["CityName"] = cityName });

        public static Error ValidationFailed(string cityName, string reason) =>
            Error.Validation(
                source: nameof(City),
                message: $"Validation failed for city [{cityName}]: {reason}",
                details: new Dictionary<string, object> { ["CityName"] = cityName, ["Reason"] = reason });
    }

    public static class Trip
    {
        public static Error NotFound(Guid tripId) =>
            Error.NotFound(
                source: nameof(Trip),
                message: $"Trip with ID [{tripId}] was not found",
                details: new Dictionary<string, object> { ["TripId"] = tripId });

        public static Error InvalidConfiguration(string reason) =>
            Error.Validation(
                source: nameof(Trip),
                message: $"Invalid trip configuration: {reason}",
                details: new Dictionary<string, object> { ["Reason"] = reason });
    }

    public static class ExternalServices
    {
        public static Error GoogleApiError(string operation, string message, bool isTransient = true) =>
            Error.ExternalService(
                source: "GoogleApi",
                message: $"Google API error during {operation}: {message}",
                recoverable: isTransient,
                details: new Dictionary<string, object> { ["Operation"] = operation });

        public static Error GeoDbApiError(string operation, string message, bool isTransient = true) =>
            Error.ExternalService(
                source: "GeoDbApi",
                message: $"GeoDB API error during {operation}: {message}",
                recoverable: isTransient,
                details: new Dictionary<string, object> { ["Operation"] = operation });

        public static Error MichelinApiError(string operation, string message, bool isTransient = true) =>
            Error.ExternalService(
                source: "MichelinApi",
                message: $"Michelin API error during {operation}: {message}",
                recoverable: isTransient,
                details: new Dictionary<string, object> { ["Operation"] = operation });

        public static Error ServiceTimeout(string serviceName, TimeSpan timeout) =>
            Error.Timeout(
                source: serviceName,
                message: $"Service [{serviceName}] timed out after [{timeout.TotalSeconds}]s",
                details: new Dictionary<string, object> { ["ServiceName"] = serviceName, ["TimeoutSeconds"] = timeout.TotalSeconds });
    }

    public static class Database
    {
        public static Error ConnectionFailed(string message) =>
            Error.ExternalService(
                source: "Database",
                message: $"Database connection failed: {message}",
                recoverable: true);

        public static Error QueryFailed(string message) =>
            Error.Internal(
                source: "Database",
                message: $"Database query failed: {message}");
    }
}
