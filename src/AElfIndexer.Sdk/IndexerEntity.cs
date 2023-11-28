using Nest;
using Volo.Abp.Domain.Entities;

namespace AElfIndexer.Sdk;

public interface IIndexerEntity
{
    Metadata Metadata { get; set; }
}

public class IndexerEntity : Entity<string>, IIndexerEntity
{
    public Metadata Metadata { get; set; }
}

public class Metadata
{
    public BlockMetadata Block { get; set; }
}

public class BlockMetadata
{
    [Keyword]
    public string ChainId { get; set; }
    [Keyword]
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    [Keyword]
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
}