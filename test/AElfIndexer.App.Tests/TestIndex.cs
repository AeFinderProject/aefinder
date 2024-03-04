using AElfIndexer.Sdk;
using JetBrains.Annotations;
using Nest;

namespace AElfIndexer.App;

public class TestIndex : IndexerEntity, IIndexerEntity
{
    public int Value { get; set; }

    public TestIndex([NotNull] string id) : base(id)
    {
    }
}