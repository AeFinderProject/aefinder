using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AElfScan.AElf.Etos;

[EventName("AElf.ConfirmTransactions")]
public class ConfirmTransactionsEto
{
    public List<ConfirmTransactionEto> ConfirmTransactions { get; set; }
}