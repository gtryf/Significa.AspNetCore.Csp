using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Significa.AspNetCore.Csp;
using Significa.AspNetCore.Csp.Middleware;

namespace Microsoft.Extensions.DependencyInjection;

public static class CspMiddlewareExtensions
{
	public static IApplicationBuilder UseCsp(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<CspMiddleware>();
	}

	public static IApplicationBuilder UseNonceInjection(this IApplicationBuilder builder)
	{
		return builder.UseMiddleware<NonceInjectionMiddleware>();
	}

	public static IServiceCollection AddCsp(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<CspConfigurationSection>(configuration);
		return services;
	}
}
