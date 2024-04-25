using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class GetSubscriptionTransactionsInput
{
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public List<FilterTransactionInput> TransactionFilters { get; set; } = new();
    public List<FilterContractEventInput> LogEventFilters { get; set; } = new();
}