using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AeFinder.App.Metrics;

public static class GraphQLHttpMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseGraphQLHttpMetrics(
        this IApplicationBuilder builder, string path = "/graphql")
    {
        return builder.UseGraphQLHttpMetrics(new PathString(path));
    }
    
    public static IApplicationBuilder UseGraphQLHttpMetrics(
        this IApplicationBuilder builder, PathString path)
    {
        return builder.UseWhen(
            context => context.Request.Path.Equals(path),
            b => b.UseMiddleware<GraphQLHttpMetricsMiddleware>());
    }
}