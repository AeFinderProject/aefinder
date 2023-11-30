using Nest;
using Volo.Abp.Domain.Entities;

namespace AElfIndexer.Sdk;

public interface IIndexerEntity
{
}

public abstract class IndexerEntity : Entity<string>
{
    [Keyword] public string Id { get; protected set; }
    public Metadata Metadata { get; set; }

    protected IndexerEntity(string id) => this.Id = id;
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