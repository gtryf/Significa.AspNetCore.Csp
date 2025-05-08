using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Significa.AspNetCore.Csp.Middleware;

public class CspMiddleware
{
	private readonly RequestDelegate _next;

	public CspMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task Invoke(HttpContext context, IOptions<CspConfigurationSection> configuration)
	{
		var cspConfig = configuration?.Value;

		if (cspConfig is null)
		{
			await _next(context);
			return;
		}

		var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

		var builder = new CspDirectiveBuilder(cspConfig, nonce);
		var cspValue = builder.Build();

		if (string.IsNullOrEmpty(cspValue))
		{
			await _next(context);
			return;
		}

		if (nonce is not null) context.Items["CSP-Nonce"] = nonce;
		context.Response.Headers[cspConfig.ReportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy"] = cspValue;

		await _next(context);
	}
}