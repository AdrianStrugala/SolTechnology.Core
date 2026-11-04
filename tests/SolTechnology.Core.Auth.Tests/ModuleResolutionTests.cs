using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace SolTechnology.Core.Auth.Tests;

[TestFixture]
public class ModuleResolutionTests
{
    private static readonly ServiceProviderOptions StrictValidation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Test]
    public async Task AddSolAuth_ResolvesAllPublicServices_WithValidation()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSolAuth(new AuthConfiguration { ApiKey = "secret" });
        await using var provider = services.BuildServiceProvider(StrictValidation);
        await using var scope = provider.CreateAsyncScope();
        var scoped = scope.ServiceProvider;

        // Act
        var schemeProvider = scoped.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme);
        var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        var handler = scoped.GetRequiredService<ApiKeyAuthenticationHandler>();
        var schemeOptions = scoped.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationSchemeOptions>>()
            .Get(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme);
        var authConfiguration = scoped.GetRequiredService<IOptions<AuthConfiguration>>().Value;
        var mvcOptions = scoped.GetRequiredService<IOptions<MvcOptions>>().Value;

        // Assert
        scheme.Should().NotBeNull();
        scheme!.HandlerType.Should().Be<ApiKeyAuthenticationHandler>();
        defaultScheme!.Name.Should().Be(ApiKeyAuthenticationSchemeOptions.AuthenticationScheme);
        handler.Should().NotBeNull();
        schemeOptions.ApiKey.Should().Be("secret");
        authConfiguration.ApiKey.Should().Be("secret");
        mvcOptions.Filters.OfType<AuthorizeFilter>().Should().ContainSingle();
    }
}
