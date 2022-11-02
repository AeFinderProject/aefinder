using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;

namespace AElf.Indexing.Elasticsearch
{
    public interface INESTWriterRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>, new()
    {
        Task AddOrUpdateAsync(TEntity model);
        
        Task AddAsync(TEntity model);
        
        Task UpdateAsync(TEntity model);

        Task DeleteAsync(TKey id);
        
        Task DeleteAsync(TEntity model);

        Task BulkAddOrUpdateAsync(List<TEntity> list, int bulkNum = 1000);

        Task BulkDelete(List<TEntity> list);
    }
}