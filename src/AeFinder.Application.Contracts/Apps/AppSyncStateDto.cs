using System.Collections.Generic;

namespace AeFinder.Apps;

public class AppSyncStateDto
{
    public List<AppSyncStateItem> Items { get; set; } = new();
}

public class AppSyncStateItem
{
    public string ChainId { get; set; }
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
}