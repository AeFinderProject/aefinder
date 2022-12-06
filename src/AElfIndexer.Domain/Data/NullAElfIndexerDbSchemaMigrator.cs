using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Data;

/* This is used if database provider does't define
 * IAElfIndexerDbSchemaMigrator implementation.
 */
public class NullAElfIndexerDbSchemaMigrator : IAElfIndexerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
