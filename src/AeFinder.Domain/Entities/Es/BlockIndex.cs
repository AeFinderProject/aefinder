using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;

namespace AeFinder.Entities.Es;

public class BlockIndex:BlockBase,IIndexBuild
{
    // public List<Transaction> Transactions {get;set;}
    public List<string> TransactionIds { get; set; }
    public int LogEventCount { get; set; }
}