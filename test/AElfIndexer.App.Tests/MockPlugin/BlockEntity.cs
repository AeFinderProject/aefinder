using AElfIndexer.Sdk;
using Nest;

namespace AElfIndexer.App.MockPlugin;

public class BlockEntity : IndexerEntity, IIndexerEntity
{
    [Keyword] public string BlockHash { get; set; }
    [Keyword] public string Miner { get; set; }
}