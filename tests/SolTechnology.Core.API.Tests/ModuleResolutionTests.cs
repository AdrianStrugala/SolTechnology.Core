using System.Diagnostics;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;
using SolTechnology.Core.API.Exceptions;
using SolTechnology.Core.API.Filters;
using SolTechnology.Core.Logging.Correlations;

namespace SolTechnology.Core.API.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolApiCore_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        AddWebHostServices(services);
        services.AddSolApiCore(o => o.IncludeExceptionDetails = true, apiTitle: "Test API");
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        object[] resolved =
        [
            scoped.GetRequiredService<ExceptionFilter>(),
            scoped.GetRequiredService<ResultConversionFilter>(),
            scoped.GetRequiredService<IProblemDetailsService>(),
            scoped.GetRequiredService<IExceptionStatusCodeMapper>(),
            scoped.GetRequiredService<ICorrelationIdService>(),
            scoped.GetRequiredService<IApiVersionDescriptionProvider>(),
            scoped.GetRequiredService<IOptions<SwaggerGenOptions>>().Value
        ];
        var options = scoped.GetRequiredService<IOptions<ApiExceptionOptions>>().Value;

        // Assert
        resolved.Should().AllSatisfy(service => service.Should().NotBeNull());
        scoped.GetServices<IConfigureOptions<SwaggerGenOptions>>().Should().ContainSingle();
        scoped.GetRequiredService<IExceptionStatusCodeMapper>().Should().BeOfType<DefaultExceptionStatusCodeMapper>();
        options.IncludeExceptionDetails.Should().BeTrue();
    }

    // MVC, which AddSolVersioning pulls in, expects the services every ASP.NET Core host registers.
    private static void AddWebHostServices(IServiceCollection services)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.ContentRootFileProvider.Returns(new NullFileProvider());
        environment.WebRootFileProvider.Returns(new NullFileProvider());
        services.AddSingleton(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        var diagnosticListener = new DiagnosticListener("SolTechnology.Core.API.Tests");
        services.AddSingleton(diagnosticListener);
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
    }
}
