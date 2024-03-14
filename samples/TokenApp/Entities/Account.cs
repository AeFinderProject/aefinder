using AElfIndexer.Sdk;
using Nest;

namespace TokenApp.Entities;

public class Account: IndexerEntity, IIndexerEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public long Amount { get; set; }
}