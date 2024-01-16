using Volo.Abp.Domain.Entities;

namespace AeFinder.Entities;

public abstract class AeFinderEntity<TKey>:Entity,IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    protected AeFinderEntity()
    {

    }

    protected AeFinderEntity(TKey id)
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
}