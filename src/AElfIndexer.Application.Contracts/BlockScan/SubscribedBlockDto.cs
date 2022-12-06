using System.Collections.Generic;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;

public class SubscribedBlockDto
{
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public string Version { get; set; }
    public BlockFilterType FilterType { get; set; }
    public List<BlockDto> Blocks { get; set; }
}