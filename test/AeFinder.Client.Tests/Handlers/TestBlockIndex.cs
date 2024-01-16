using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AeFinder.Client.Handlers;

public class TestBlockIndex : AeFinderClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword] public string SignerPubkey { get; set; }
    [Keyword] public string Signature { get; set; }
}