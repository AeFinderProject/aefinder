using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Client.Handlers;

public interface IBlockChainDataHandler<T>
{
    BlockFilterType FilterType { get; }
    Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos);
}