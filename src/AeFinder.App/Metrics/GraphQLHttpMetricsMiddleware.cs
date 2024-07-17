using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Timing;

namespace AeFinder.App.Metrics;

public class GraphQLHttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Counter<long> _graphQLRequestsCounter;
    private readonly IClock _clock;

    public GraphQLHttpMetricsMiddleware(RequestDelegate next, Instrumentation instrumentation, IClock clock)
    {
        _next = next;
        _graphQLRequestsCounter = instrumentation.GraphQLRequestsCounter;
        _clock = clock;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _graphQLRequestsCounter.Add(1, _graphQLRequestsCounter!.Tags!.First(),
            new KeyValuePair<string, object>("date", _clock.Now.Date.ToString("yyyy-MM-dd")));
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}