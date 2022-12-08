using System.Collections.Generic;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.BlockScan;

public class SubscribeInfo
{
    public string ChainId { get; set; }
    public long StartBlockNumber{get;set;}
    public bool OnlyConfirmedBlock { get; set; }
    public BlockFilterType FilterType { get; set; } = BlockFilterType.Block;
    public List<FilterContractEventInput> SubscribeEvents {get;set;}= new();
}