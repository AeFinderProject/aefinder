using System.Threading.Tasks;

namespace AElfScan.Data;

public interface IAElfScanDbSchemaMigrator
{
    Task MigrateAsync();
}
