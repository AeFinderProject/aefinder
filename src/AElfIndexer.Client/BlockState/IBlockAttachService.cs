using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Client.BlockState;

public interface IBlockAttachService
{
    Task AttachBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks);
}