using System.Diagnostics;
using System.Diagnostics.Metrics;
using AeFinder.Metrics;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Timing;

namespace AeFinder.App.Metrics;

public class GraphQLHttpMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Counter<long> _graphQLRequestsCounter;
    private readonly IClock _clock;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IElapsedTimeRecorder _elapsedTimeRecorder;

    public GraphQLHttpMetricsMiddleware(RequestDelegate next, Instrumentation instrumentation, IClock clock,
        IAppInfoProvider appInfoProvider, IElapsedTimeRecorder elapsedTimeRecorder)
    {
        _next = next;
        _graphQLRequestsCounter = instrumentation.GraphQLRequestsCounter;
        _clock = clock;
        _appInfoProvider = appInfoProvider;
        _elapsedTimeRecorder = elapsedTimeRecorder;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _graphQLRequestsCounter.Add(1, _graphQLRequestsCounter!.Tags!.First(),
            new KeyValuePair<string, object>("date", _clock.Now.Date.ToString("yyyy-MM-dd")),
            new KeyValuePair<string, object>("monitor", _appInfoProvider.AppId));
        var stopwatch = Stopwatch.StartNew();
        // Call the next delegate/middleware in the pipeline.
        await _next(context);
        stopwatch.Stop();
        _elapsedTimeRecorder.Record("AeFinderAppHostGraphQLRequests",stopwatch.ElapsedMilliseconds);
    }
}