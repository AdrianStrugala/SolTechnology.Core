﻿namespace SolTechnology.Core.HTTP
{
    /// <summary>
    /// Connection settings of one named HTTP client, bound from <c>HTTPClients:{name}</c> or passed
    /// to <c>AddSolHTTPClient</c>.
    /// </summary>
    public class HTTPClientConfiguration
    {
        /// <summary>
        /// Top-level configuration section under which per-client subsections live
        /// (e.g. <c>HTTPClients:football-data</c>).
        /// </summary>
        public const string SectionName = "HTTPClients";

        /// <summary>
        /// Suffix appended to a per-client section to find its custom-options
        /// subsection (e.g. <c>HTTPClients:football-data:Options</c>).
        /// </summary>
        public const string OptionsSubSection = "Options";

        /// <summary>
        /// Suffix appended to a per-client section to find its resilience-policy
        /// override (e.g. <c>HTTPClients:football-data:Policy</c>).
        /// </summary>
        public const string PolicySubSection = "Policy";

        /// <summary>
        /// Absolute base URI of the upstream. Required; the host refuses to start without it.
        /// </summary>
        public required string BaseAddress { get; set; }

        /// <summary>
        /// <see cref="HttpClient.Timeout"/> in seconds, used only when
        /// <see cref="HttpPolicyConfiguration.UsePolly"/> is <c>false</c>. With Polly the per-attempt
        /// <see cref="HttpPolicyConfiguration.RequestTimeout"/> applies and a warning is logged.
        /// </summary>
        public int? TimeoutSeconds { get; set; }

        /// <summary>
        /// Default headers added to every request of the client.
        /// </summary>
        public List<Header> Headers { get; set; } = new List<Header>();
    }

    /// <summary>
    /// A default request header of an HTTP client.
    /// </summary>
    public class Header
    {
        /// <summary>Header name.</summary>
        public required string Name { get; set; }

        /// <summary>Header value.</summary>
        public required string Value { get; set; }
    }
}
