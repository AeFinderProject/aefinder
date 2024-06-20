using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace AeFinder.App.Metrics;

public class GraphQLHttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Counter<long> _graphQLRequestsCounter;

    public GraphQLHttpMetricsMiddleware(RequestDelegate next, Instrumentation instrumentation)
    {
        _next = next;
        _graphQLRequestsCounter = instrumentation.GraphQLRequestsCounter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _graphQLRequestsCounter.Add(1,_graphQLRequestsCounter!.Tags!.First());
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}

