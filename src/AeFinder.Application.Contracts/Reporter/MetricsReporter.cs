using Prometheus;

namespace AeFinder.Reporter;

public static class MetricsReporter
{
    public static Gauge RegistryGauges(string gaugeName, string[] labels, string help = "")
        => Metrics.CreateGauge(gaugeName, help, labels);

    public static Counter RegistryCounters(string counterName, string[] labels, string help = "")
        => Metrics.CreateCounter(counterName, help, labels);

    public static Histogram RegistryHistograms(string histogramName, string[] labels, string help = "")
        => Metrics.CreateHistogram(histogramName, help, labels);

    public static Summary RegistrySummaries(string summaryName, string[] labels, string help = "")
        => Metrics.CreateSummary(summaryName, help, labels);
}