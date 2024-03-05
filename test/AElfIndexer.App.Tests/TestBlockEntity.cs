using AElfIndexer.Sdk;
using Nest;

namespace AElfIndexer.App;

public class TestBlockEntity : IndexerEntity, IIndexerEntity
{
    [Keyword] public string BlockHash { get; set; }
    [Keyword] public string Miner { get; set; }
}