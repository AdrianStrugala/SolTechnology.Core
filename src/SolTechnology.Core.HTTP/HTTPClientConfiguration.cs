﻿namespace SolTechnology.Core.HTTP
{
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

        public required string BaseAddress { get; set; }
        public int? TimeoutSeconds { get; set; }
        public List<Header> Headers { get; set; } = new List<Header>();
    }

    public class Header
    {
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
