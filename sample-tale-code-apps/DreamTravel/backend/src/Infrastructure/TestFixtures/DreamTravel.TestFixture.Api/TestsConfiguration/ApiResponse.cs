using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DreamTravel.TestFixture.Api.TestsConfiguration
{
    public class ApiResponse<T>
    {
        private T _deserializedObject;

        public T GetBody()
        {
            if (_deserializedObject == null)
            {
                _deserializedObject = JsonConvert.DeserializeObject<T>(RawBody);
            }

            return _deserializedObject;
        }

        public Dictionary<string, string[]> Headers { get; set; }

        public string RawBody { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public string[] GetValidationErrorForField(string fieldName)
        {
            var jToken = JObject.Parse(RawBody)["errors"];

            if (jToken == null)
            {
                throw new Exception($"There is no error?! Got RawBody as follows: [{RawBody}]");
            }
                
            var fieldJToken = jToken[fieldName];

            if (fieldJToken == null)
            {
                throw new Exception($"There is no error for field [{fieldName}]?! But got errors as follow: [{jToken}]");
            }

            return fieldJToken.Children().Select(s => s.ToString()).ToArray();
        }
    }

    public class EmptyResponse
    {

    }
}