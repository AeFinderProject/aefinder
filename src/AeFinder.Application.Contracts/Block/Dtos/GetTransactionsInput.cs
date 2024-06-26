using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class GetTransactionsInput
{
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public List<FilterContractEventInput> Events { get; set; }
    public string TransactionId { get; set; }
}
