using System.Collections.Generic;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.BlockScan;

public class SubscribedBlockDto
{
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public string Version { get; set; }
    public List<BlockWithTransactionDto> Blocks { get; set; }
    public string Token { get; set; }
}