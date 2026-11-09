namespace SolTechnology.Core.Hangfire;

/// <summary>
/// Options for the recurring jobs registered with <c>AddSolRecurringJob&lt;TJob&gt;()</c>; set them with
/// <c>services.Configure&lt;RecurringJobsOptions&gt;(...)</c>.
/// </summary>
public sealed class RecurringJobsOptions
{
    /// <summary>
    /// When <c>true</c>, at start the module removes the recurring jobs this application
    /// (<c>IHostEnvironment.ApplicationName</c>) registered before and no longer registers. Enable it only when every
    /// instance of the application registers the same jobs and no other application registers the same ids under the
    /// same name. Default <c>false</c>: such jobs stay until removed from the dashboard.
    /// </summary>
    public bool RemoveStaleRecurringJobs { get; set; }
}
