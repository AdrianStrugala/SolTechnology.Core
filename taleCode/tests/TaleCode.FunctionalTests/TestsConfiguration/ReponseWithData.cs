using System.Net.Http;

namespace TaleCode.ComponentTests.TestsConfiguration
{
    public record ResponseWithData<T>(HttpResponseMessage Response, T Data);
}
