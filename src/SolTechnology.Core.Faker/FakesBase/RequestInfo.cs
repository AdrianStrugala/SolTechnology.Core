namespace SolTechnology.Core.Faker.FakesBase
{
    public class RequestInfo
    {
        public RequestInfo(
            HttpMethod httpMethod, 
            string path, 
            Dictionary<string, string>? queryParameters = null)
        {
            QueryParameters = queryParameters ?? new Dictionary<string, string>();
            Path = path;
            HttpMethod = httpMethod;
        }

        public string Path { get; set; }
        public Dictionary<string, string> QueryParameters { get; set; }
        public HttpMethod HttpMethod { get; set; }
    }
}
