using AElf.Indexing.Elasticsearch;
using Newtonsoft.Json;
using Volo.Abp.Domain.Entities;

namespace AElfIndexer.Client;

public abstract class AElfIndexerClientEntity<TKey> : Entity, IEntity<TKey>, IIndexBuild
{
    public virtual TKey Id { get; set; }
    
    public string ChainId { get; set; }
    
    public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    public string PreviousBlockHash { get; set; }
    
    public bool IsConfirmed { get; set; }

    protected AElfIndexerClientEntity()
    {

    }

    protected AElfIndexerClientEntity(TKey id)
    {
        Id = id;
    }

    public override object[] GetKeys()
    {
        return new object[] {Id};
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }

    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this);
    }
}