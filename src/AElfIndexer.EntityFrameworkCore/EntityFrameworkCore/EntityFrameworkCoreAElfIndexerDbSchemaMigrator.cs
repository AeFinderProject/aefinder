using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AElfIndexer.Data;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.EntityFrameworkCore;

public class EntityFrameworkCoreAElfIndexerDbSchemaMigrator
    : IAElfIndexerDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAElfIndexerDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the AElfIndexerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AElfIndexerDbContext>()
            .Database
            .MigrateAsync();
    }
}
