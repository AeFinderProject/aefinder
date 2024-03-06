using AElfIndexer.Sdk;
using Nest;

namespace AElfIndexer.App.MockPlugin;

public class AccountBalanceEntity: IndexerEntity, IIndexerEntity
{
    [Keyword] public string Account { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public long Amount { get; set; }
}