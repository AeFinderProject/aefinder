using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class GetTransactionsInput
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool HasLogEvent { get; set; } = false;
    public List<ContractInput> Contracts { get; set; }
}
