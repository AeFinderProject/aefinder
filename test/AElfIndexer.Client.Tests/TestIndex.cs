using System;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace AElfIndexer.Client;

public class TestIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public int Value { get; set; }
}