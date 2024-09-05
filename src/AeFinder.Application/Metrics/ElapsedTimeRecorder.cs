using System.Collections.Generic;
using System.Diagnostics.Metrics;
using AElf.OpenTelemetry;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Metrics;

public class ElapsedTimeRecorder : IElapsedTimeRecorder,ISingletonDependency
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Histogram<long>> _histogramMapCache = new ();

    public ElapsedTimeRecorder(IInstrumentationProvider instrumentationProvider)
    {
        _meter = instrumentationProvider.Meter;
    }

    public void Record(string recordName, long elapsedMilliseconds)
    {
        var histogram = GetHistogram(recordName);

        histogram.Record(elapsedMilliseconds);
    }
    
    private Histogram<long> GetHistogram(string recordName)
    {
        var key = $"{recordName}.elapsed.time";

        if (_histogramMapCache.TryGetValue(key, out var rtKeyCache))
        {
            return rtKeyCache;
        }
        
        var histogram = _meter.CreateHistogram<long>(
            name: key,
            description: "Histogram for action elapsed time",
            unit: "ms"
        );
        _histogramMapCache.Add(key, histogram);
        return histogram;
    }
}