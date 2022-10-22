using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElfScan.ScanClients;

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

public class Block
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
}