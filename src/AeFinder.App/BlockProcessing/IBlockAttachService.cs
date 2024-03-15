using AeFinder.Block.Dtos;

namespace AeFinder.App.BlockProcessing;

public interface IBlockAttachService
{
    Task AttachBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks);
}