using System.Collections.Generic;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.BlockScan;

public class SubscribedBlockDto
{
    public string ChainId { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
    public List<BlockWithTransactionDto> Blocks { get; set; }
    public string PushToken { get; set; }
}