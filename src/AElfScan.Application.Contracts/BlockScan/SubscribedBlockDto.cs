using System.Collections.Generic;
using AElfScan.Block.Dtos;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class SubscribedBlockDto
{
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public string Version { get; set; }
    public List<BlockDto> Blocks { get; set; }
}