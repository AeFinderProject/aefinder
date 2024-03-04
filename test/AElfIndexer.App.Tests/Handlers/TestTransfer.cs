using AElfIndexer.Sdk;
using JetBrains.Annotations;
using Nest;

namespace AElfIndexer.App.Handlers;

public class TestTransfer : IndexerEntity, IIndexerEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }

    public TestTransfer([NotNull] string id) : base(id)
    {
    }
}