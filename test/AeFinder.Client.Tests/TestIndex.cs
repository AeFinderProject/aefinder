using AElf.Indexing.Elasticsearch;
using Nest;

namespace AeFinder.Client;

public class TestIndex : AeFinderClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public int Value { get; set; }
}