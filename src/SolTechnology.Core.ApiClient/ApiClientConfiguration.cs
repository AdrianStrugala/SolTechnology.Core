namespace SolTechnology.Core.ApiClient
{
    public class ApiClientConfiguration
    {
        public string Name { get; set; }
        public string BaseAddress { get; set; }
        public int? TimeoutSeconds { get; set; }
        public List<Header> Headers { get; set; } = new List<Header>();
    }

    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}