using System.Text.Json.Serialization;

namespace SolTechnology.Core.Api
{
    public class Response<T>
    {
        public bool IsSuccess { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public T Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Error { get; set; }
    }

    public class Response
    {
        public bool IsSuccess { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Error { get; set; }
    }
}