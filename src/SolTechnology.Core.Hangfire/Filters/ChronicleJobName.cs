using Hangfire.Common;

namespace SolTechnology.Core.Hangfire.Filters;

/// <summary>Names a job's Chronicle root after the work, not after the Hangfire plumbing that runs it.</summary>
internal static class ChronicleJobName
{
    public static string Of(Job job)
    {
        if (job.Type.IsGenericType && job.Type.GetGenericTypeDefinition() == typeof(RecurringJobRunner<>))
        {
            return "Job " + job.Type.GetGenericArguments()[0].Name;
        }

        if (job.Type == typeof(HangfireEventPublisher) && job.Args is [{ } @event, ..])
        {
            return "Event " + @event.GetType().Name;
        }

        return "Job " + job.Type.Name + "." + job.Method.Name;
    }
}
