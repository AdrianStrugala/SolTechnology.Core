using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace DreamTravel.FunctionalTests;

public class ContractTests
{
    private ISwaggerProvider _swaggerProvider;

    [SetUp]
    public void Setup()
    {
        var scope = IntegrationTestsFixture.ApiFixture.TestServer.Services.CreateScope();
        _swaggerProvider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
    }
    
    /// <summary>
    /// This test requires https://plugins.jetbrains.com/plugin/17240-verify-support
    /// It will fail when a change to the API is introduced
    /// Then you need to right click the test, review changes and accept them eventually
    /// A short video on the plugin page
    /// </summary>
    
    [Test]
    public Task ContractTest_reviewChangesToTheApi()
    {
        OpenApiDocument doc = _swaggerProvider.GetSwagger("v1", null, "/");
        string swaggerFile = doc.SerializeAsYaml(Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
        
        return Verify(swaggerFile);
    }
}