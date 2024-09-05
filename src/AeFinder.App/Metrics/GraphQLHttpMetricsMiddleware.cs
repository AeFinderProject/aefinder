using System.Diagnostics.Metrics;
using AElf.OpenTelemetry.ExecutionTime;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Timing;

namespace AeFinder.App.Metrics;

[AggregateExecutionTime]
public class GraphQLHttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Counter<long> _graphQLRequestsCounter;
    private readonly IClock _clock;
    private readonly IAppInfoProvider _appInfoProvider;

    public GraphQLHttpMetricsMiddleware(RequestDelegate next, Instrumentation instrumentation, IClock clock, IAppInfoProvider appInfoProvider)
    {
        _next = next;
        _graphQLRequestsCounter = instrumentation.GraphQLRequestsCounter;
        _clock = clock;
        _appInfoProvider = appInfoProvider;
    }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        _graphQLRequestsCounter.Add(1, _graphQLRequestsCounter!.Tags!.First(),
            new KeyValuePair<string, object>("date", _clock.Now.Date.ToString("yyyy-MM-dd")),
            new KeyValuePair<string, object>("monitor", _appInfoProvider.AppId));
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}