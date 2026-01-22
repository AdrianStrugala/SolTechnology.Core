using DreamTravel.Domain.Errors;
using SolTechnology.Core.CQRS;

namespace DreamTravel.Infrastructure.Hangfire;

/// <summary>
/// Interface for Hangfire jobs that return structured error information.
/// </summary>
/// <typeparam name="TRequest">The type of the job request.</typeparam>
public interface IJob<in TRequest>
{
    Task<Result<JobError>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Hangfire jobs without input that return structured error information.
/// </summary>
public interface IJob
{
    Task<Result<JobError>> ExecuteAsync(CancellationToken cancellationToken = default);
}
