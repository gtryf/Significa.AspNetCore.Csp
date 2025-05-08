using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Significa.AspNetCore.Csp.Middleware;

namespace Significa.AspNetCore.Csp.Tests;

public class CspMiddlewareTests
{
    private static RequestDelegate NextMock => _ => Task.CompletedTask;

    [Fact]
    public async Task Invoke_WithNullConfiguration_DoesNotAddCspHeader()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var options = Options.Create<CspConfigurationSection>(null!);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Invoke_WithEmptyConfiguration_DoesNotAddCspHeader()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var options = Options.Create(new CspConfigurationSection());

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Invoke_WithEmptySource_AddsNone()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection { Default = new CspSource() };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Contains("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.DoesNotContain("'nonce-", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task Invoke_AddsNonceInContext()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection { Default = new CspSource() };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.True(context.Items.ContainsKey("CSP-Nonce"));
    }

    [Fact]
    public async Task Invoke_WithNullSource_DoesNotAdd()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection { Default = new CspSource() };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.DoesNotContain("frame-src", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task Invoke_WithReportOnlyEnabled_AddsCspReportOnlyHeader()
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection
        {
            ReportOnly = true,
            Default = new CspSource()
        };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy-Report-Only"));
    }

    [Theory]
    [InlineData(true, "block-all-mixed-content")]
    [InlineData(false, null)]
    public async Task Invoke_WithBlockMixedContentSetting_AddsCorrectHeader(bool blockMixedContent, string expectedHeaderValue)
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection
        {
            BlockMixedContent = blockMixedContent
        };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        if (expectedHeaderValue != null)
        {
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
            Assert.Contains(expectedHeaderValue, context.Response.Headers["Content-Security-Policy"].ToString());
        }
        else
        {
            Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        }
    }

    [Theory]
    [InlineData(true, "upgrade-insecure-requests")]
    [InlineData(false, null)]
    public async Task Invoke_WithUpgradeInsecureRequestsSetting_AddsCorrectHeader(bool upgradeInsecureRequests, string expectedHeaderValue)
    {
        // Arrange
        var middleware = new CspMiddleware(NextMock);
        var context = new DefaultHttpContext();
        var config = new CspConfigurationSection
        {
            UpgradeInsecureRequests = upgradeInsecureRequests
        };
        var options = Options.Create(config);

        // Act
        await middleware.Invoke(context, options);

        // Assert
        if (expectedHeaderValue != null)
        {
            Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
            Assert.Contains(expectedHeaderValue, context.Response.Headers["Content-Security-Policy"].ToString());
        }
        else
        {
            Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        }
    }
}
