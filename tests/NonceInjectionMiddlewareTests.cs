using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Significa.AspNetCore.Csp.Middleware;
using System.Text;
using Moq;

namespace Significa.AspNetCore.Csp.Tests;

public class NonceInjectionMiddlewareTests
{
    [Fact]
    public async Task Invoke_WithNonHtmlResponse_DoesNotModifyContent()
    {
        // Arrange
        var options = Options.Create(new CspConfigurationSection() { NoncePlaceholder = "{nonce}" });
        var middleware = new NonceInjectionMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Response.ContentType = "application/json";

        // Add a nonce to HttpContext.Items
        var nonce = "test-nonce-value";
        context.Items["CSP-Nonce"] = nonce;

        // Write some content to the response
        var content = "{\"key\":\"value\"}";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        await responseBody.WriteAsync(contentBytes, 0, contentBytes.Length);
        responseBody.Position = 0;

        // Act
        await middleware.Invoke(context);

        // Assert
        responseBody.Position = 0;
        var resultContent = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Equal(content, resultContent);
        // Ensure the content was not modified with the nonce
        Assert.DoesNotContain(nonce, resultContent);
    }

    [Fact]
    public async Task Invoke_WithHtmlResponse_InjectsNonceIntoContent()
    {
        // Arrange
        var options = Options.Create(new CspConfigurationSection() { NoncePlaceholder = "{nonce}" });
        var nextMock = new Mock<RequestDelegate>();
        var context = new DefaultHttpContext();
        var content = "<script nonce=\"{nonce}\">console.log('test');</script>";
        nextMock.Setup(next => next(context)).Returns(async () =>
        {
            // Write some HTML content with nonce placeholder to the response
            var contentBytes = Encoding.UTF8.GetBytes(content);
            await context.Response.Body.WriteAsync(contentBytes, 0, contentBytes.Length);
        });

        var middleware = new NonceInjectionMiddleware(nextMock.Object, options);
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Response.ContentType = "text/html; charset=utf-8";

        // Add a nonce to HttpContext.Items
        var nonce = "test-nonce-value";
        context.Items["CSP-Nonce"] = nonce;

        // Act
        await middleware.Invoke(context);

        // Assert
        responseBody.Position = 0;
        var resultContent = await new StreamReader(responseBody).ReadToEndAsync();

        // Ensure the content was modified with the nonce
        Assert.Contains($"nonce=\"{nonce}\"", resultContent);
        Assert.DoesNotContain("{nonce}", resultContent);
        Assert.Equal(content.Replace("{nonce}", nonce), resultContent);
    }

    [Fact]
    public async Task Invoke_WithHtmlResponse_InjectsNonceIntoContent_SplitChunks()
    {
        // Arrange
        var options = Options.Create(new CspConfigurationSection() { NoncePlaceholder = "{nonce}" });
        var nextMock = new Mock<RequestDelegate>();
        var context = new DefaultHttpContext();
        var contentPart1 = new string('a', 8187) + "<script nonce=\"{no";
        var contentPart2 = "nce}\">console.log('test');</script>" + new string('b', 8187);
        nextMock.Setup(next => next(context)).Returns(async () =>
        {
            // Write some HTML content with nonce placeholder split across two chunks to the response
            var contentBytes1 = Encoding.UTF8.GetBytes(contentPart1);
            await context.Response.Body.WriteAsync(contentBytes1, 0, contentBytes1.Length);
            var contentBytes2 = Encoding.UTF8.GetBytes(contentPart2);
            await context.Response.Body.WriteAsync(contentBytes2, 0, contentBytes2.Length);
        });

        var middleware = new NonceInjectionMiddleware(nextMock.Object, options);
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Response.ContentType = "text/html; charset=utf-8";

        // Add a nonce to HttpContext.Items
        var nonce = "test-nonce-value";
        context.Items["CSP-Nonce"] = nonce;

        // Act
        await middleware.Invoke(context);

        // Assert
        responseBody.Position = 0;
        var resultContent = await new StreamReader(responseBody).ReadToEndAsync();

        // Ensure the content was modified with the nonce
        Assert.Contains($"nonce=\"{nonce}\"", resultContent);
        Assert.DoesNotContain("{nonce}", resultContent);
        Assert.Equal((contentPart1 + contentPart2).Replace("{nonce}", nonce), resultContent);
    }

    [Theory]
    [InlineData("text/html; charset=utf-8", "utf-8")]
    [InlineData("text/html; charset=utf-16", "utf-16")]
    [InlineData("text/html; charset=iso-8859-1", "iso-8859-1")]
    [InlineData("text/html", "utf-8")] // Default encoding
    public async Task Invoke_WithDifferentEncodings_InjectsNonceCorrectly(string contentType, string encodingName)
    {
        // Arrange
        var options = Options.Create(new CspConfigurationSection() { NoncePlaceholder = "{nonce}" });
        var nextMock = new Mock<RequestDelegate>();
        var context = new DefaultHttpContext();
        var content = "<script nonce=\"{nonce}\">console.log('test');</script>";
        nextMock.Setup(next => next(context)).Returns(async () =>
        {
            // Write some HTML content with nonce placeholder to the response
            var contentBytes = Encoding.GetEncoding(encodingName).GetBytes(content);
            await context.Response.Body.WriteAsync(contentBytes, 0, contentBytes.Length);
        });

        var middleware = new NonceInjectionMiddleware(nextMock.Object, options);
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Response.ContentType = contentType;

        // Add a nonce to HttpContext.Items
        var nonce = "test-nonce-value";
        context.Items["CSP-Nonce"] = nonce;

        // Act
        await middleware.Invoke(context);

        // Assert
        responseBody.Position = 0;
        var resultContent = await new StreamReader(responseBody, Encoding.GetEncoding(encodingName)).ReadToEndAsync();

        // Ensure the content was modified with the nonce
        Assert.Contains($"nonce=\"{nonce}\"", resultContent);
        Assert.DoesNotContain("{nonce}", resultContent);
        Assert.Equal(content.Replace("{nonce}", nonce), resultContent);
    }
}