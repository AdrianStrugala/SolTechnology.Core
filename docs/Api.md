### Overview

The SolTechnology.Core.Api library provides API utilities and filters for ASP.NET Core applications. It includes exception handling middleware, response envelope filters, and testing utilities for API integration tests.

### Registration

For installing the library, reference **SolTechnology.Core.Api** nuget package.

### Configuration

No configuration is needed.

### Usage

1) Exception Handling Middleware

Add the exception handler middleware to automatically handle exceptions in your API:

```csharp
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

2) Response Envelope Filter

Add the response envelope filter to wrap your API responses in a consistent format:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ResponseEnvelopeFilter>();
});
```

3) API Testing

Use the ApiFixture class for integration testing your API:

```csharp
public class MyApiTests
{
    private readonly ApiFixture _fixture;

    public MyApiTests()
    {
        _fixture = new ApiFixture();
    }

    [Test]
    public async Task TestMyEndpoint()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/myendpoint");
        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
```
