using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class GetTransactionsInput
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public List<EventInput> Events { get; set; }
}
