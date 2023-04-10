using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Client.Handlers;

public interface IBlockChainDataHandler
{
    BlockFilterType FilterType { get; }
    Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos);
}