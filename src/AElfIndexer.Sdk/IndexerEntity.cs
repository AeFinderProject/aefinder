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
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
}