using AeFinder.Reporter;
using Prometheus;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Grains.Reporter;

public interface IScanReporter
{
    void RecordScanBlock(string client, double latency, long size);

    void RecordScanHeight(string client, long height);
}

public class ScanReporter : IScanReporter, ISingletonDependency
{
    private readonly Histogram _scanBlockLatencyHistogram;
    private readonly Histogram _scanBlockBatchSizeHistogram;
    private readonly Counter _scanBlockHeightCounter;

    public ScanReporter()
    {
        _scanBlockLatencyHistogram = MetricsReporter.RegistryHistograms(DefinitionConstants.ScanBlockBatch, new[] { DefinitionConstants.Client, DefinitionConstants.Latency });
        _scanBlockBatchSizeHistogram = MetricsReporter.RegistryHistograms(DefinitionConstants.ScanBlockBatch, new[] { DefinitionConstants.Client, DefinitionConstants.BatchSize });
        _scanBlockHeightCounter = MetricsReporter.RegistryCounters(DefinitionConstants.ScanBlockBatch, new[] { DefinitionConstants.Client, DefinitionConstants.Height });
    }


    public void RecordScanBlock(string client, double latency, long size)
    {
        _scanBlockLatencyHistogram.WithLabels(client, DefinitionConstants.Latency).Observe(latency);
        _scanBlockBatchSizeHistogram.WithLabels(client, DefinitionConstants.BatchSize).Observe(size);
    }

    public void RecordScanHeight(string client, long height)
    {
        _scanBlockHeightCounter.WithLabels(client, DefinitionConstants.BatchSize).IncTo(height);
    }
}

public static class DefinitionConstants
{
    public const string ScanBlockBatch = "scan_block_batch";
    public const string ScanTransactionBatch = "scan_transaction_batch";
    public const string ScanLogEventBatch = "scan_log_event_batch";
    public const string Client = "client";
    public const string Latency = "latency";
    public const string BatchSize = "batch_size";
    public const string Height = "height";
}