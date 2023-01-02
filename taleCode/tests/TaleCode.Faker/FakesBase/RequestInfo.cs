namespace TaleCode.Faker.FakesBase
{
    public class RequestInfo
    {
        public RequestInfo(HttpMethod httpMethod, string path)
        {
            Path = path;
            HttpMethod = httpMethod;
        }

        public string Path { get; set; }
        public HttpMethod HttpMethod { get; set; }
    }
}
