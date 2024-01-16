using System.Collections.Generic;
using AeFinder.Entities.Es;
using Volo.Abp.EventBus;

namespace AeFinder.Etos;

[EventName("NewBlock")]
public class NewBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}