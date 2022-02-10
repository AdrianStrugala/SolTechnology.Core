namespace SolTechnology.Core.ApiClient
{
    public class ApiClientConfiguration
    {
        public List<HttpClient> HttpClients { get; set; } = new List<HttpClient>();
    }

    public class HttpClient
    {
        public string Name { get; set; }
        public string BaseAddress { get; set; }
        public int? TimeoutSeconds { get; set; }
        public DataType DataType { get; set; }

        public List<Header> Headers { get; set; } = new List<Header>();
    }


    public enum DataType
    {
        Json = 0,
        Avro = 1
    }

    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}