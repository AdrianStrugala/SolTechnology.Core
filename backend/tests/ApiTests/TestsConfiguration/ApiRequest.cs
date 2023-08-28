using System.Collections.Generic;

namespace DreamTravel.ApiTests.TestsConfiguration
{
    public class ApiRequest<T>
    {
        public T Body { get; set; }

        public KeyValuePair<string, string> Headers { get; set; }

        public string RawBody { get; set; }

        public int HttpStatusCode { get; set; }
    }
}