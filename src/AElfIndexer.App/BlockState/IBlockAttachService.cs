using AElfIndexer.Block.Dtos;

namespace AElfIndexer.App.BlockState;

public interface IBlockAttachService
{
    Task AttachBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks);
}