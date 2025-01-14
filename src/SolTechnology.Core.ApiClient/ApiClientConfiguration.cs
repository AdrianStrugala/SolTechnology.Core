namespace SolTechnology.Core.ApiClient
{
    public class ApiClientConfiguration
    {
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