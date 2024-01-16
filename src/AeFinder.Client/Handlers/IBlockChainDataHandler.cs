using AeFinder.Block.Dtos;

namespace AeFinder.Client.Handlers;

public interface IBlockChainDataHandler
{
    BlockFilterType FilterType { get; }
    Task HandleBlockChainDataAsync(string chainId, string clientId, List<BlockWithTransactionDto> blockDtos);
}