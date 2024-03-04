using System;
using AElfIndexer.Sdk;
using JetBrains.Annotations;
using Nest;

namespace AElfIndexer.App.Handlers;

public class TestBlockIndex : IndexerEntity, IIndexerEntity
{
    public DateTime BlockTime { get; set; }
    [Keyword] public string SignerPubkey { get; set; }
    [Keyword] public string Signature { get; set; }

    public TestBlockIndex([NotNull] string id) : base(id)
    {
    }
}