using System.Net;
using Apotheca.Api.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Apotheca.Api.Tests.Providers;

[TestFixture]
public class NetworkProviderTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private NetworkProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _provider = new NetworkProvider(_httpContextAccessor);
    }

    [Test]
    public void GetClientIpAddress_ReturnsNull_WhenHttpContextIsNull()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetClientIpAddress_ReturnsCfConnectingIp_WhenHeaderPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["CF-Connecting-IP"] = "1.2.3.4";
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.EqualTo("1.2.3.4"));
    }

    [Test]
    public void GetClientIpAddress_SkipsCfConnectingIp_WhenHeaderIsWhitespace()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["CF-Connecting-IP"] = "   ";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("9.9.9.9");
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.EqualTo("9.9.9.9"));
    }

    [Test]
    public void GetClientIpAddress_ReturnsXForwardedFor_WhenCfConnectingIpNotPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "5.6.7.8";
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.EqualTo("5.6.7.8"));
    }

    [Test]
    public void GetClientIpAddress_ReturnsFirstIp_WhenXForwardedForContainsMultiple()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "5.6.7.8, 10.0.0.1, 192.168.1.1";
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.EqualTo("5.6.7.8"));
    }

    [Test]
    public void GetClientIpAddress_ReturnsRemoteIpAddress_WhenNoProxyHeaders()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.0.1");
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.EqualTo("192.168.0.1"));
    }

    [Test]
    public void GetClientIpAddress_ReturnsNull_WhenNoHeadersAndNoRemoteIp()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var result = _provider.GetClientIpAddress();

        Assert.That(result, Is.Null);
    }
}
