using System.Collections.Generic;

namespace AElfIndexer.Block.Dtos;

public class GetBlocksInput
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public bool HasTransaction { get; set; } = false;
    public List<FilterContractEventInput> Events { get; set; }
}