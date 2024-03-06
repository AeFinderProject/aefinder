using AElfIndexer.Block.Dtos;

namespace AElfIndexer.App.BlockProcessing;

public interface IBlockAttachService
{
    Task AttachBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks);
}