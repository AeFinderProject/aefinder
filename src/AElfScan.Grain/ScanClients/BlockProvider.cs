using AElfScan.Grain.Contracts.ScanClients;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Grain.ScanClients;

public interface IBlockProvider
{
    Task<List<Block>> GetBlockAsync(long start, long end);
}
public class BlockProvider:IBlockProvider,ITransientDependency
{
    public async Task<List<Block>> GetBlockAsync(long start, long end)
    {
        var result = new List<Block>();
        for (long i = start; i <= end; i++)
        {
            result.Add(new Block
            {
                BlockHeight = i,
                BlockHash = Guid.NewGuid().ToString()
            });
        }

        return result;
    }
}