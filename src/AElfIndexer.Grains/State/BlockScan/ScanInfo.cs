using System;

namespace AElfIndexer.Grains.State.BlockScan;

public class ScanInfo
{
    public string Version { get; set; }
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public string ScanToken { get; set; }
}