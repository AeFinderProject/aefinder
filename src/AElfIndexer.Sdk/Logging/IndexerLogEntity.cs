using Microsoft.Extensions.Logging;

namespace AElfIndexer.Sdk.Logging;

public class IndexerLogEntity : IndexerEntity
{
    public string ClientId { get; set; }
    public string Version { get; set; }
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; }

    public IndexerLogEntity(string id) : base(id)
    {
    }
}