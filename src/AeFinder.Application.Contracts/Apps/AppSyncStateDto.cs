using System.Collections.Generic;
using Orleans;

namespace AeFinder.Apps;

[GenerateSerializer]
public class AppSyncStateDto
{
    [Id(0)] public AppVersionSyncState CurrentVersion { get; set; }
    [Id(1)] public AppVersionSyncState PendingVersion { get; set; }
    
}

[GenerateSerializer]
public class AppVersionSyncState
{
    [Id(0)] public string Version { get; set; }
    [Id(1)] public List<AppSyncStateItem> Items { get; set; } = new();
}

[GenerateSerializer]
public class AppSyncStateItem
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string LongestChainBlockHash { get; set; }
    [Id(2)] public long LongestChainHeight { get; set; }
    [Id(3)] public string BestChainBlockHash { get; set; }
    [Id(4)] public long BestChainHeight { get; set; }
    [Id(5)] public string LastIrreversibleBlockHash { get; set; }
    [Id(6)] public long LastIrreversibleBlockHeight { get; set; }
}