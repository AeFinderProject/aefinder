using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public interface INESTRepository<TEntity, TKey> : INESTReaderRepository<TEntity, TKey>,
        INESTWriterRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
    }
}