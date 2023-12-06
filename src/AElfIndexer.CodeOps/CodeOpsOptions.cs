namespace AElfIndexer.CodeOps;

public class CodeOpsOptions
{
    public int AuditTimeoutDuration { get; set; } = Constants.DefaultAuditTimeoutDuration;
    public int MaxEntityCount { get; set; } = Constants.DefaultMaxEntityCount;
}