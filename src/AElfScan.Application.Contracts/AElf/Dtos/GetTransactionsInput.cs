using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class GetTransactionsInput
{
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool HasLogEvent { get; set; } = false;
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }
}