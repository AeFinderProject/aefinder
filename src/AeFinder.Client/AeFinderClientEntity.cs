using Nest;
using Newtonsoft.Json;
using Volo.Abp.Domain.Entities;

namespace AeFinder.Client;

public class AeFinderClientEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }
    
    [Keyword]public string ChainId { get; set; }
    
    [Keyword]public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    
    public bool IsDeleted { get; set; }

    protected AeFinderClientEntity()
    {

    }

    protected AeFinderClientEntity(TKey id)
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