using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Data;

/* This is used if database provider does't define
 * IAeFinderDbSchemaMigrator implementation.
 */
public class NullAeFinderDbSchemaMigrator : IAeFinderDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
