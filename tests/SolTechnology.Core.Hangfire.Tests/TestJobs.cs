using SolTechnology.Core.Errors;

namespace SolTechnology.Core.Hangfire.Tests
{
    public class TestJob : IJob
    {
        public virtual Task<Result> Execute(CancellationToken cancellationToken) => Result.SuccessAsTask();
    }

    public class AnotherTestJob : IJob
    {
        public virtual Task<Result> Execute(CancellationToken cancellationToken) => Result.SuccessAsTask();
    }

    public class GenericJob<T> : IJob
    {
        public Task<Result> Execute(CancellationToken cancellationToken) => Result.SuccessAsTask();
    }

    public class RecoverableFailingJob : IJob
    {
        public Task<Result> Execute(CancellationToken cancellationToken) =>
            Result.FailAsTask(new Error { Message = "Upstream timeout", Recoverable = true });
    }

    public class ThrowingJob : IJob
    {
        public Task<Result> Execute(CancellationToken cancellationToken) => throw new InvalidOperationException("boom");
    }
}

namespace SolTechnology.Core.Hangfire.Tests.Other
{
    public class TestJob : IJob
    {
        public Task<Result> Execute(CancellationToken cancellationToken) => Result.SuccessAsTask();
    }
}
