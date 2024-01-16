using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Client.Handlers;

public interface IBlockChainDataHandler
{
    Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos);
}