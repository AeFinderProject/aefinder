using Volo.Abp.Domain.Entities;

namespace AElfScan.Entities;

public abstract class AElfScanEntity<TKey>:Entity,IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    protected AElfScanEntity()
    {

    }

    protected AElfScanEntity(TKey id)
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