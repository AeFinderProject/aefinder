using AElf.EntityMapping.Entities;
using AElfIndexer.Entities;
using Nest;

namespace AElfIndexer.Sdk;

public interface IIndexerEntity
{
}

public abstract class IndexerEntity : AElfIndexerEntity<string>, IEntityMappingEntity
{
    [Keyword]public override string Id { get; set; }
    public Metadata Metadata { get; set; } = new ();
}

public class Metadata
{
    [Keyword]
    public string ChainId { get; set; }
    public BlockMetadata Block { get; set; }
    public bool IsDeleted { get; set; }
}

public class BlockMetadata
{
    [Keyword]
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}