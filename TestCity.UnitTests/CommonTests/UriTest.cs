using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.UnitTests.CommonTests;

public class UriTest(ITestOutputHelper output)
{
    [Fact]
    public void Parse_ValidHttpUrl_ReturnsCorrectUri()
    {
        const string urlString = "https://example.com/path?query=value#fragment";

        var uri = new Uri(urlString);

        Assert.Equal("https", uri.Scheme);
        Assert.Equal("example.com", uri.Host);
        Assert.Equal("/path", uri.AbsolutePath);
        Assert.Equal("?query=value", uri.Query);
        Assert.Equal("#fragment", uri.Fragment);
    }

    [Fact]
    public void Parse_UrlWithPort_ParsesPortCorrectly()
    {
        const string urlString = "http://localhost:8080/api";

        var uri = new Uri(urlString);

        Assert.Equal("http", uri.Scheme);
        Assert.Equal("localhost", uri.Host);
        Assert.Equal(8080, uri.Port);
        Assert.Equal("/api", uri.AbsolutePath);
    }

    [Fact]
    public void Parse_RelativeUrl_ThrowsUriFormatException()
    {
        const string relativeUrl = "path/to/resource";

        Assert.Throws<UriFormatException>(() => new Uri(relativeUrl));
    }

    [Fact]
    public void Parse_RelativeUrl_WithBaseUri_CreatesCorrectAbsoluteUri()
    {
        var baseUri = new Uri("https://example.com");
        const string relativeUrl = "path/to/resource";

        var uri = new Uri(baseUri, relativeUrl);

        Assert.Equal("https://example.com/path/to/resource", uri.AbsoluteUri);
    }

    [Fact]
    public void Test_ToString_WithSlashes()
    {
        Assert.Equal("https://example.com/", new Uri("https://example.com").ToString());
        Assert.Equal("https://example.com/", new Uri("https://example.com/").ToString());
        Assert.Equal("https://example.com/path", new Uri("https://example.com/path").ToString());
        Assert.Equal("https://example.com/path/", new Uri("https://example.com/path/").ToString());
        Assert.Equal("https://example.com/path", new Uri(new Uri("https://example.com/"), "path").ToString());
        Assert.Equal("https://example.com/path/", new Uri(new Uri("https://example.com/"), "path/").ToString());
        Assert.Equal("https://example.com/x/path", new Uri(new Uri("https://example.com/x"), "path").ToString());
        Assert.Equal("https://example.com/x/path/", new Uri(new Uri("https://example.com/x/"), "path/").ToString());
    }

    [Fact]
    public void ToString_ReturnsOriginalUrl()
    {
        const string originalUrl = "https://example.com/path?query=value#fragment";
        var uri = new Uri(originalUrl);

        var result = uri.ToString();

        Assert.Equal(originalUrl, result);
    }

    [Fact]
    public void Parse_MalformedUrl_ThrowsUriFormatException()
    {
        const string malformedUrl = "http://exam ple.com";
        Assert.Throws<UriFormatException>(() => new Uri(malformedUrl));
    }

    [Fact]
    public void StringInterpolation_WithUri_CallsToString()
    {
        var uri = new Uri("https://example.com/path");
        var result = $"URI: {uri}";
        Assert.Equal("URI: https://example.com/path", result);
    }

    [Fact]
    public void Parse_UrlWithCredentials_ParsesUserInfoCorrectly()
    {
        const string urlWithCredentials = "https://username:password@example.com";

        var uri = new Uri(urlWithCredentials);

        Assert.Equal("username:password", uri.UserInfo);
        Assert.Equal("example.com", uri.Host);
    }

    [Fact]
    public void TryCreate_ValidUrl_ReturnsTrue()
    {
        const string urlString = "https://example.com";
        Uri? resultUri = null;

        var success = Uri.TryCreate(urlString, UriKind.Absolute, out resultUri);

        Assert.True(success);
        Assert.NotNull(resultUri);
        Assert.Equal("https://example.com/", resultUri!.AbsoluteUri);
        Assert.Equal("https", resultUri!.Scheme);
        Assert.Equal("example.com", resultUri!.Host);
    }

    [Fact]
    public void TryCreate_InvalidUrl_ReturnsFalse()
    {
        const string invalidUrl = "http:///invalid";
        Uri? resultUri = null;

        var success = Uri.TryCreate(invalidUrl, UriKind.Absolute, out resultUri);

        Assert.False(success);
        Assert.Null(resultUri);
    }

    [Fact]
    public void StringTemplate_WithUri_UsesToString()
    {
        var uri = new Uri("https://example.com/path");

        var result = $"Link to {uri}";

        Assert.Equal("Link to https://example.com/path", result);
    }

    [Fact]
    public void UriComponents_ReturnsSpecificParts()
    {
        var uri = new Uri("https://user:pass@example.com:8080/path/file.html?query=value#fragment");

        // Act & Assert
        Assert.Equal("https", uri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped));
        Assert.Equal("user:pass", uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped));
        Assert.Equal("example.com", uri.GetComponents(UriComponents.Host, UriFormat.Unescaped));
        Assert.Equal("8080", uri.GetComponents(UriComponents.Port, UriFormat.Unescaped));
        Assert.Equal("path/file.html", uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        Assert.Equal("/path/file.html", uri.AbsolutePath);
        Assert.Equal("query=value", uri.GetComponents(UriComponents.Query, UriFormat.Unescaped));
        Assert.Equal("fragment", uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped));
    }

    [Fact]
    public void ToString_AbsoluteUri_OriginalString_CompareResults()
    {
        const string originalString = "https://example.com/path?query=value#fragment";
        var uri = new Uri(originalString);

        var toStringResult = uri.ToString();
        var absoluteUriResult = uri.AbsoluteUri;
        var originalStringResult = uri.OriginalString;

        Assert.Equal(originalString, toStringResult);
        Assert.Equal(originalString, absoluteUriResult);
        Assert.Equal(originalString, originalStringResult);
    }

    [Fact]
    public void Equals_SameUri_ReturnsTrue()
    {
        var uri1 = new Uri("https://example.com/path");
        var uri2 = new Uri("https://example.com/path");

        // Act & Assert
        Assert.Equal(uri2, uri1);
        Assert.True(uri1.Equals(uri2));
    }

    [Fact]
    public void UrlEncoding_HandlesSpecialCharacters()
    {
        const string urlWithSpecialChars = "https://example.com/path with spaces?query=value with spaces";

        var uri = new Uri(urlWithSpecialChars);

        Assert.Equal("https://example.com/path%20with%20spaces?query=value%20with%20spaces", uri.AbsoluteUri);
        Assert.Equal(urlWithSpecialChars, uri.OriginalString);
    }

    private readonly ILogger logger = GlobalSetup.TestLoggerFactory(output).CreateLogger(nameof(UriTest));
}
