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

        var previousHash = Guid.NewGuid().ToString();
        for (long i = start; i <= end; i++)
        {
            var blockHash = Guid.NewGuid().ToString();
            result.Add(new BlockDto
            {
                BlockNumber = i,
                BlockHash = blockHash,
                PreviousBlockHash = previousHash
            });

            previousHash = blockHash;
        }

        return result;
    }
}