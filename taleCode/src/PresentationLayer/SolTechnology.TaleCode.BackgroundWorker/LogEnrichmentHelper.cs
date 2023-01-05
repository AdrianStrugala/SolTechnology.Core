using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace SolTechnology.TaleCode.BackgroundWorker;

[ExcludeFromCodeCoverage]
public static class LogEnrichmentHelper
{
    public static void EnrichLogs(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        httpContext.Request.Body.Position = 0;
        diagnosticContext.Set("Headers",
            "{" + string.Join(",",
                httpContext.Request.Headers.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}");
        diagnosticContext.Set("Body", ParseBody(httpContext.Request.Body));
    }

    private static string ParseBody(Stream body)
    {
        var bodyJson = new StreamReader(body).ReadToEndAsync().Result;
        return string.IsNullOrEmpty(bodyJson) ? "no-body" : JObject.Parse(bodyJson).ToString(Formatting.None);
    }
}
