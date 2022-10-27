using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class GetBlocksInput
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool IsConfirmed { get; set; }
    public bool HasTransaction { get; set; } = false;
    public List<ContractInput> Contracts { get; set; }
}