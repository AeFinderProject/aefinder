using AElfScan.AElf.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public interface IBlockProvider
{
    Task<List<BlockDto>> GetBlockAsync(long start, long end);
}
public class BlockProvider:IBlockProvider,ITransientDependency
{
    public async Task<List<BlockDto>> GetBlockAsync(long start, long end)
    {
        var result = new List<BlockDto>();
        for (long i = start; i <= end; i++)
        {
            result.Add(new BlockDto
            {
                BlockNumber = i,
                BlockHash = Guid.NewGuid().ToString()
            });
        }

        return result;
    }
}