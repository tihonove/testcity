using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace TestCity.UnitTests.CommonTests;

public class UriTest
{
    [Test]
    public void Parse_ValidHttpUrl_ReturnsCorrectUri()
    {
        const string urlString = "https://example.com/path?query=value#fragment";

        var uri = new Uri(urlString);

        Assert.That(uri.Scheme, Is.EqualTo("https"));
        Assert.That(uri.Host, Is.EqualTo("example.com"));
        Assert.That(uri.AbsolutePath, Is.EqualTo("/path"));
        Assert.That(uri.Query, Is.EqualTo("?query=value"));
        Assert.That(uri.Fragment, Is.EqualTo("#fragment"));
    }

    [Test]
    public void Parse_UrlWithPort_ParsesPortCorrectly()
    {
        const string urlString = "http://localhost:8080/api";

        var uri = new Uri(urlString);

        Assert.That(uri.Scheme, Is.EqualTo("http"));
        Assert.That(uri.Host, Is.EqualTo("localhost"));
        Assert.That(uri.Port, Is.EqualTo(8080));
        Assert.That(uri.AbsolutePath, Is.EqualTo("/api"));
    }

    [Test]
    public void Parse_RelativeUrl_ThrowsUriFormatException()
    {
        const string relativeUrl = "path/to/resource";

        Assert.That(() => new Uri(relativeUrl), Throws.InstanceOf<UriFormatException>());
    }

    [Test]
    public void Parse_RelativeUrl_WithBaseUri_CreatesCorrectAbsoluteUri()
    {
        var baseUri = new Uri("https://example.com");
        const string relativeUrl = "path/to/resource";

        var uri = new Uri(baseUri, relativeUrl);

        Assert.That(uri.AbsoluteUri, Is.EqualTo("https://example.com/path/to/resource"));
    }

    [Test]
    public void ToString_ReturnsOriginalUrl()
    {
        const string originalUrl = "https://example.com/path?query=value#fragment";
        var uri = new Uri(originalUrl);

        var result = uri.ToString();

        Assert.That(result, Is.EqualTo(originalUrl));
    }

    [Test]
    public void Parse_MalformedUrl_ThrowsUriFormatException()
    {
        const string malformedUrl = "http://exam ple.com";
        Assert.That(() => new Uri(malformedUrl), Throws.InstanceOf<UriFormatException>());
    }

    [Test]
    public void StringInterpolation_WithUri_CallsToString()
    {
        var uri = new Uri("https://example.com/path");
        var result = $"URI: {uri}";
        Assert.That(result, Is.EqualTo("URI: https://example.com/path"));
    }

    [Test]
    public void Parse_UrlWithCredentials_ParsesUserInfoCorrectly()
    {
        const string urlWithCredentials = "https://username:password@example.com";

        var uri = new Uri(urlWithCredentials);

        Assert.That(uri.UserInfo, Is.EqualTo("username:password"));
        Assert.That(uri.Host, Is.EqualTo("example.com"));
    }

    [Test]
    public void TryCreate_ValidUrl_ReturnsTrue()
    {
        const string urlString = "https://example.com";
        Uri? resultUri = null;

        var success = Uri.TryCreate(urlString, UriKind.Absolute, out resultUri);

        Assert.That(success, Is.True);
        Assert.That(resultUri, Is.Not.Null);
        Assert.That(resultUri!.AbsoluteUri, Is.EqualTo("https://example.com/"));
        Assert.That(resultUri!.Scheme, Is.EqualTo("https"));
        Assert.That(resultUri!.Host, Is.EqualTo("example.com"));
    }

    [Test]
    public void TryCreate_InvalidUrl_ReturnsFalse()
    {
        const string invalidUrl = "http:///invalid";
        Uri? resultUri = null;

        var success = Uri.TryCreate(invalidUrl, UriKind.Absolute, out resultUri);

        Assert.That(success, Is.False);
        Assert.That(resultUri, Is.Null);
    }

    [Test]
    public void StringTemplate_WithUri_UsesToString()
    {
        var uri = new Uri("https://example.com/path");

        var result = $"Link to {uri}";

        Assert.That(result, Is.EqualTo("Link to https://example.com/path"));
    }

    [Test]
    public void UriComponents_ReturnsSpecificParts()
    {
        var uri = new Uri("https://user:pass@example.com:8080/path/file.html?query=value#fragment");

        // Act & Assert
        Assert.That(uri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped), Is.EqualTo("https"));
        Assert.That(uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped), Is.EqualTo("user:pass"));
        Assert.That(uri.GetComponents(UriComponents.Host, UriFormat.Unescaped), Is.EqualTo("example.com"));
        Assert.That(uri.GetComponents(UriComponents.Port, UriFormat.Unescaped), Is.EqualTo("8080"));
        Assert.That(uri.GetComponents(UriComponents.Path, UriFormat.Unescaped), Is.EqualTo("path/file.html"));
        Assert.That(uri.AbsolutePath, Is.EqualTo("/path/file.html"));
        Assert.That(uri.GetComponents(UriComponents.Query, UriFormat.Unescaped), Is.EqualTo("query=value"));
        Assert.That(uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped), Is.EqualTo("fragment"));
    }

    [Test]
    public void ToString_AbsoluteUri_OriginalString_CompareResults()
    {
        const string originalString = "https://example.com/path?query=value#fragment";
        var uri = new Uri(originalString);

        var toStringResult = uri.ToString();
        var absoluteUriResult = uri.AbsoluteUri;
        var originalStringResult = uri.OriginalString;

        Assert.That(toStringResult, Is.EqualTo(originalString));
        Assert.That(absoluteUriResult, Is.EqualTo(originalString));
        Assert.That(originalStringResult, Is.EqualTo(originalString));
    }

    [Test]
    public void Equals_SameUri_ReturnsTrue()
    {
        var uri1 = new Uri("https://example.com/path");
        var uri2 = new Uri("https://example.com/path");

        // Act & Assert
        Assert.That(uri1, Is.EqualTo(uri2));
        Assert.That(uri1.Equals(uri2), Is.True);
    }

    [Test]
    public void UrlEncoding_HandlesSpecialCharacters()
    {
        const string urlWithSpecialChars = "https://example.com/path with spaces?query=value with spaces";

        var uri = new Uri(urlWithSpecialChars);

        Assert.That(uri.AbsoluteUri, Is.EqualTo("https://example.com/path%20with%20spaces?query=value%20with%20spaces"));
        Assert.That(uri.OriginalString, Is.EqualTo(urlWithSpecialChars));
    }

    private readonly ILogger logger = GlobalSetup.TestLoggerFactory.CreateLogger(nameof(UriTest));
}
