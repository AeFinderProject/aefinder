using System.Diagnostics.Metrics;
using Volo.Abp.Guids;

namespace AeFinder.App.Metrics;

public class Instrumentation : IDisposable
{
    internal const string MeterName = "AeFinder.App.Host";
    private readonly Meter _meter;

    public Instrumentation(IGuidGenerator guidGenerator)
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        _meter = new Meter(MeterName, version);
        GraphQLRequestsCounter = _meter.CreateCounter<long>("graphql.requests", null, "The number of graphQL requests",
            new[] { new KeyValuePair<string, object>("id", guidGenerator.Create().ToString()) });
    }
    
    public Counter<long> GraphQLRequestsCounter { get; }

    public void Dispose()
    {
        _meter.Dispose();
    }
}