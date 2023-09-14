using System.Net.Http;

namespace TaleCode.FunctionalTests.TestsConfiguration
{
    public record ResponseWithData<T>(HttpResponseMessage Response, T Data);
}
