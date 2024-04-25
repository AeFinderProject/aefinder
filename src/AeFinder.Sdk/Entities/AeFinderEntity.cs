using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Sdk.Entities;

public interface IAeFinderEntity
{
}

public abstract class AeFinderEntity : AeFinderEntity<string>, IEntityMappingEntity
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