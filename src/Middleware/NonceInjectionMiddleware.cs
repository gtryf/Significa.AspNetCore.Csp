using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;
using System.Linq;

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

            // Intercept the response so we can evaluate headers AFTER downstream has run
            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                // Run the rest of the pipeline
                await _next(context);

                // Rewind the captured response
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Only process html responses after downstream has set headers
                if (!IsHtmlResponse(context.Response.ContentType))
                {
                    await memoryStream.CopyToAsync(originalBodyStream);
                    return;
                }

                // If server sent 304/204, avoid serving cached HTML with stale nonce; force no-cache going forward
                if (context.Response.StatusCode == StatusCodes.Status304NotModified ||
                    context.Response.StatusCode == StatusCodes.Status204NoContent)
                {
                    SetNoCacheHeaders(context.Response);
                    await memoryStream.CopyToAsync(originalBodyStream);
                    return;
                }

                // Detect the original encoding
                var originalEncoding = DetectEncoding(context.Response.ContentType) ?? Encoding.UTF8;
                using var reader = new StreamReader(memoryStream, originalEncoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var originalResponseBody = await reader.ReadToEndAsync();

                // Replace the placeholder with the nonce
                var newResponseBody = originalResponseBody.Replace(_noncePlaceholder, nonce);

                // Write the updated response and ensure it won't be cached (nonce is per-request)
                var newResponseBytes = originalEncoding.GetBytes(newResponseBody);
                context.Response.Body = originalBodyStream;
                context.Response.ContentLength = newResponseBytes.Length;
                SetNoCacheHeaders(context.Response);
                await context.Response.Body.WriteAsync(newResponseBytes, 0, newResponseBytes.Length);
            }
            finally
            {
                // Ensure the response body stream is restored
                context.Response.Body = originalBodyStream;
            }
        }

        private static void SetNoCacheHeaders(HttpResponse response)
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
            response.Headers.Remove("ETag");
            response.Headers.Remove("Last-Modified");
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
    }
}
