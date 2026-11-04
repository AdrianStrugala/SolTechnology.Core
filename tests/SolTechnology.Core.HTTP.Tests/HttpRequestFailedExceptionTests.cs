using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace SolTechnology.Core.HTTP.Tests;

[TestFixture]
public class HttpRequestFailedExceptionTests
{
    [TestCase("https://h/p?key=SECRET", "https://h/p", TestName = "{m}(absolute URI with query)")]
    [TestCase("https://user:SECRET@h/p#SECRET", "https://h/p", TestName = "{m}(user info and fragment)")]
    [TestCase("p/q?key=SECRET", "p/q", TestName = "{m}(relative URI with query)")]
    public void HttpRequestFailedException_Message_OmitsQuery(string uri, string expectedUriInMessage)
    {
        // Arrange
        var requestUri = new Uri(uri, UriKind.RelativeOrAbsolute);

        // Act
        var exception = new HttpRequestFailedException(HttpMethod.Get, requestUri, HttpStatusCode.BadGateway, "Bad Gateway", responseBody: null);

        // Assert
        exception.Message.Should().Be($"HTTP GET {expectedUriInMessage} failed with status 502 Bad Gateway.");
        exception.ToString().Should().NotContain("SECRET");
        exception.RequestUri.Should().Be(requestUri);
    }
}
