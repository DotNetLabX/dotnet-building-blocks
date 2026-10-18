using Microsoft.AspNetCore.Builder;

namespace Blocks.AspNetCore.Middlewares;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
