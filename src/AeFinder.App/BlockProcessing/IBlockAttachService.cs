using AeFinder.Block.Dtos;
using AeFinder.BlockScan;

namespace AeFinder.App.BlockProcessing;

public interface IBlockAttachService
{
    Task AttachBlocksAsync(string chainId, List<AppSubscribedBlockDto> blocks);
}