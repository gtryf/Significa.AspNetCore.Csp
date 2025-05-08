using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace Significa.AspNetCore.Csp.Middleware
{
    public class NonceInjectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _noncePlaceholder;

        public NonceInjectionMiddleware(RequestDelegate next, IOptions<CspConfigurationSection> configuration)
        {
            _next = next;
            _noncePlaceholder = configuration.Value?.NoncePlaceholder ?? string.Empty;
        }

        public async Task Invoke(HttpContext context)
        {
            var nonce = context.Items["CSP-Nonce"]?.ToString();
            if (string.IsNullOrEmpty(_noncePlaceholder) || string.IsNullOrEmpty(nonce))
            {
                await _next(context);
                return;
            }

            if (!ShouldProcessResponse(context.Response))
            {
                await _next(context);
                return;
            }

            // Intercept the response to inject the nonce into index.html
            var originalBodyStream = context.Response.Body;
            using (var memoryStream = new MemoryStream())
            {
                // Redirect the response body to the memory stream
                context.Response.Body = memoryStream;

                // Call the next middleware in the pipeline
                await _next(context);

                // Reset the memory stream position to read the response
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Detect the original encoding
                var originalEncoding = DetectEncoding(context.Response.ContentType) ?? Encoding.UTF8;
                using var reader = new StreamReader(memoryStream, originalEncoding);
                var originalResponseBody = await reader.ReadToEndAsync();

                // Replace the placeholder in index.html with the nonce
                var newResponseBody = originalResponseBody.Replace(_noncePlaceholder, nonce);

                // Set the new response body
                var newResponseBytes = originalEncoding.GetBytes(newResponseBody);
                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = newResponseBytes.Length;
                context.Response.ContentType = "text/html"; // Set the appropriate content type
                await context.Response.Body.WriteAsync(newResponseBytes, 0, newResponseBytes.Length);
            }
        }

        private Encoding DetectEncoding(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return Encoding.UTF8;
            }

            var charset = contentType.Split(';')
                                     .Select(part => part.Trim())
                                     .FirstOrDefault(part => part.StartsWith("charset=", StringComparison.OrdinalIgnoreCase));

            if (charset != null)
            {
                var encodingName = charset.Substring("charset=".Length);
                try
                {
                    return Encoding.GetEncoding(encodingName);
                }
                catch (ArgumentException)
                {
                    // Invalid encoding specified, fall back to default
                }
            }

            return Encoding.UTF8;
        }

        private bool IsHtmlResponse(string contentType)
            => contentType != null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);

        private bool ShouldProcessResponse(HttpResponse response) =>
            IsHtmlResponse(response.ContentType) &&
            response.StatusCode != StatusCodes.Status304NotModified &&
            response.StatusCode != StatusCodes.Status204NoContent;
    }
}
