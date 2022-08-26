using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Data;

/* This is used if database provider does't define
 * IAElfScanDbSchemaMigrator implementation.
 */
public class NullAElfScanDbSchemaMigrator : IAElfScanDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
