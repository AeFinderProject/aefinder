using System;
using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.State.BlockScan;

public class BlockScanInfoState
{
    public ClientInfo ClientInfo { get; set; } = new();
    public SubscriptionItem SubscriptionItem {get;set;}= new();
    public Guid MessageStreamId { get; set; }
}