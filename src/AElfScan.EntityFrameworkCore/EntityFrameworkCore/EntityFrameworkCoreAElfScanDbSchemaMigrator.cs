using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AElfScan.Data;
using Volo.Abp.DependencyInjection;

namespace AElfScan.EntityFrameworkCore;

public class EntityFrameworkCoreAElfScanDbSchemaMigrator
    : IAElfScanDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAElfScanDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the AElfScanDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AElfScanDbContext>()
            .Database
            .MigrateAsync();
    }
}
