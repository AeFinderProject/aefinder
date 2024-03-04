using Nest;
using Newtonsoft.Json;
using Volo.Abp.Domain.Entities;

namespace AElfIndexer.Sdk;

public interface IIndexerEntity
{
}

public abstract class IndexerEntity : Entity<string>
{
    [Keyword] public string Id { get; set; }
    public Metadata Metadata { get; set; } = new Metadata();
    
    protected IndexerEntity(string id) => this.Id = id;
    
    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this);
    }
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