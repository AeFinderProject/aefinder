using Nest;
using Newtonsoft.Json;
using Volo.Abp.Domain.Entities;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }
    
    [Keyword]public string ChainId { get; set; }
    
    [Keyword]public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    
    public bool IsDeleted { get; set; }

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