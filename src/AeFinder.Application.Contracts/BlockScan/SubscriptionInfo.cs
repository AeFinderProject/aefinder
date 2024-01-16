using System.Collections.Generic;
using AeFinder.Block.Dtos;

namespace AeFinder.BlockScan;

public class SubscriptionInfo
{
    public string ChainId { get; set; }
    public long StartBlockNumber{get;set;}
    public bool OnlyConfirmedBlock { get; set; }
    public BlockFilterType FilterType { get; set; } = BlockFilterType.Block;
    public List<FilterContractEventInput> SubscribeEvents {get;set;}= new();
}