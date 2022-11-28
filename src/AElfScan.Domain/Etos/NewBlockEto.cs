using System.Collections.Generic;
using AElfScan.Entities.Es;
using Volo.Abp.EventBus;

namespace AElfScan.Etos;

[EventName("AElf.NewBlock")]
public class NewBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}