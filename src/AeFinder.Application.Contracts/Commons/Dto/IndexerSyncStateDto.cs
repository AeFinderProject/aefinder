using System.Collections.Generic;

namespace AeFinder.Commons.Dto;

public class IndexerSyncStateDto
{
    public SyncStateVersionDto CurrentVersion { get; set; }
    public SyncStateVersionDto PendingVersion { get; set; } 
}



