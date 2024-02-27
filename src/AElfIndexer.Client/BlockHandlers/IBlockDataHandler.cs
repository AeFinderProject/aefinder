using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Client.BlockHandlers;

public interface IBlockDataHandler
{
    Task HandleBlockChainDataAsync(string chainId, List<BlockWithTransactionDto> blockDtos);
}