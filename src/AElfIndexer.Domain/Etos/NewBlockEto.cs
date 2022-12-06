using System.Collections.Generic;
using AElfIndexer.Entities.Es;
using Volo.Abp.EventBus;

namespace AElfIndexer.Etos;

[EventName("AElf.NewBlock")]
public class NewBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}