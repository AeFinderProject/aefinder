using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace AeFinder;

[Serializable]
public abstract class AeFinderDomainEntity : IEntity
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Keys = {GetKeys().JoinAsString(", ")}";
    }

    public abstract object[] GetKeys();

    public bool EntityEquals(IEntity other)
    {
        return EntityHelper.EntityEquals(this, other);
    }
}

[Serializable]
public abstract class AeFinderDomainEntity<TKey> : AeFinderDomainEntity, IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}
