using System.Threading.Tasks;

namespace AElfIndexer.Data;

public interface IAElfIndexerDbSchemaMigrator
{
    Task MigrateAsync();
}
