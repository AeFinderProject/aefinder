using System.Collections.Generic;
using AElfScan.Entities.Es;
using Volo.Abp.EventBus;

namespace AElfScan.Etos;

public class ConfirmBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}