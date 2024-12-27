namespace AeFinder.GraphQL.Dto;

public class IndexerSyncStateDto
{
    public SyncStateVersionDto CurrentVersion { get; set; }
    public SyncStateVersionDto PendingVersion { get; set; } 
}