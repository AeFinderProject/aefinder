using System.Collections.Generic;

namespace AeFinder.Studio;

public class AppBlockStateMonitorDto
{
    public List<MonitorBlockState> CurrentVersionBlockStates { get; set; }
    public List<MonitorBlockState> NewVersionBlockStates { get; set; }
}

public class MonitorBlockState
{
    public string ChainId { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
}