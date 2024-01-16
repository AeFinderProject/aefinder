using System.Collections.Generic;
using AeFinder.Entities.Es;

namespace AeFinder.Etos;

public class ConfirmBlockEto:BlockBase
{
    public List<Transaction> Transactions {get;set;}
}