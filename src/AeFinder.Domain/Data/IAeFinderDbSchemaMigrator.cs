using System.Threading.Tasks;

namespace AeFinder.Data;

public interface IAeFinderDbSchemaMigrator
{
    Task MigrateAsync();
}
