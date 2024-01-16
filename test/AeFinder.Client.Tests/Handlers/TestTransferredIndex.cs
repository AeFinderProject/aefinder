using AElf.Indexing.Elasticsearch;
using Nest;

namespace AeFinder.Client.Handlers;

public class TestTransferredIndex : AeFinderClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }
}