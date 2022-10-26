using AElfScan.AElf.Etos;
using Volo.Abp.DependencyInjection;

namespace AElfScan;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(NewBlockEto block);
    Task ProcessConfirmBlocksAsync(List<ConfirmBlockEto> confirmBlocks);
}

public class BlockIndexHandler : IBlockIndexHandler, ITransientDependency
{
    public Task ProcessNewBlockAsync(NewBlockEto block)
    {
        throw new NotImplementedException();
    }

    public Task ProcessConfirmBlocksAsync(List<ConfirmBlockEto> confirmBlocks)
    {
        throw new NotImplementedException();
    }
}