using AElfIndexer.Sdk;
using Nest;

namespace AElfIndexer.App;

public class TestTransferEntity : IndexerEntity, IIndexerEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }
}