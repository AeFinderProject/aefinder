using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace AElfIndexer.Handler;

public class TestTransferredIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }
}