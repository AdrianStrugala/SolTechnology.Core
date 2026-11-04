## SolTechnology.Core.Auth

API-key authentication for ASP.NET Core APIs in one call: every MVC endpoint requires the key in the
`X-API-KEY` header unless it is marked `[AllowAnonymous]`.

Formerly `SolTechnology.Core.Authentication` (1.x). In 2.0 the package, assembly, and namespace are `SolTechnology.Core.Auth`; `AddSolAuthentication` → `AddSolAuth`, `AuthenticationConfiguration` → `AuthConfiguration`.

### Features

- **One-call setup** — `AddSolAuth` registers the `ApiKey` scheme as the default scheme and a global
  MVC filter that requires an authenticated user.
- **Fail-fast configuration** — a missing or blank key throws at registration, before the host starts.
- **Constant-time comparison** — both keys are hashed with SHA-256 and compared with
  `CryptographicOperations.FixedTimeEquals`, so response time does not reveal how much of the key matched.
- **Safe by default** — a blank configured key never authenticates.
- **No key material in logs** — rejected requests log a `Warning` with the request path only.
- **Opt-out per endpoint** — `[AllowAnonymous]` skips the check.

### Registration

Bind the key in `Program.cs`, pass it to `AddSolAuth`, and add authentication before authorization:

```csharp
var authConfiguration = builder.Configuration.GetRequiredSection("Authentication").Get<AuthConfiguration>()!;
builder.Services.AddSolAuth(authConfiguration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### Configuration

The package binds no configuration section; the host passes an `AuthConfiguration`:

| Option | Default | Purpose |
|---|---|---|
| `AuthConfiguration.ApiKey` | none (required) | The key every request sends in `X-API-KEY`. `AddSolAuth` throws `ArgumentException` when it is null, empty or whitespace. |

```json
{
  "Authentication": {
    "ApiKey": "<from a secret store, never committed>"
  }
}
```

### Usage

#### Calling the API

Send the raw key in the `X-API-KEY` header:

```csharp
httpClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName, apiKey);
var response = await httpClient.GetAsync("api/trips");
```

| Request | Outcome |
|---|---|
| Correct key | Authenticated; `User.Identity.Name` is `ApiKey` |
| Missing header | `401 Unauthorized`; Warning "Missing authentication header" |
| Wrong key (any length) | `401 Unauthorized`; Warning "Invalid API key" |
| Configured key is blank (`ApiKeyAuthenticationSchemeOptions.ApiKey`) | `401 Unauthorized` for every request; Warning "No API key is configured" |
| Endpoint marked `[AllowAnonymous]` | No authentication result; the request runs |

#### Anonymous endpoints

Mark the action or controller with `[AllowAnonymous]`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public IActionResult Get() => Ok("alive");
}
```

#### Scheme options

`AddSolAuth` copies the key into `ApiKeyAuthenticationSchemeOptions.ApiKey` for the `ApiKey` scheme. Read
or replace it through the named options:

```csharp
builder.Services.PostConfigure<ApiKeyAuthenticationSchemeOptions>(
    ApiKeyAuthenticationSchemeOptions.AuthenticationScheme,
    options => options.ApiKey = rotatedKey);
```

### Testing

Boot the API with `SolTechnology.Core.API.Testing` and add the header to the client:

```csharp
[Test]
public async Task Get_WithApiKey_ReturnsOk()
{
    // Arrange
    var configuration = new TestConfigurationBuilder()
        .Override("Authentication:ApiKey", "test-key")
        .Build();
    using var fixture = new APIFixture<Program>(configuration);
    var client = fixture.CreateAnonymousClient();
    client.DefaultRequestHeaders.Add(ApiKeyAuthenticationSchemeOptions.AuthenticationHeaderName, "test-key");

    // Act
    var response = await client.GetAsync("/api/trips");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

`CreateAuthorizedClient(scheme, token)` sets the `Authorization` header, which this scheme does not read.

### Conventions

- Keep the key out of `appsettings.json` in source control; load it from environment variables or a
  secret store.
- Call `UseAuthentication()` before `UseAuthorization()`.
- Use API keys for machine-to-machine calls only; they carry no user identity, expiry or rotation.
- Do not log the key or the `X-API-KEY` header.

### What ships in DI

- `AuthenticationOptions` with `ApiKey` as the default scheme, handled by `ApiKeyAuthenticationHandler`.
- `ApiKeyAuthenticationSchemeOptions` named `ApiKey`, holding the configured key.
- `IOptions<AuthConfiguration>` holding the configured key.
- A global `AuthorizeFilter` on `MvcOptions` that requires an authenticated user.
