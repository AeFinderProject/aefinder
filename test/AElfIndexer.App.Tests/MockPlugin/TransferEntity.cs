using AElfIndexer.Sdk;
using Nest;

namespace AElfIndexer.App.MockPlugin;

public class TransferEntity : IndexerEntity, IIndexerEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }
}