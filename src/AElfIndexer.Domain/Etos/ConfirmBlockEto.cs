using System.Collections.Generic;
using AElfIndexer.Entities.Es;

namespace AElfIndexer.Etos;

public class ConfirmBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}