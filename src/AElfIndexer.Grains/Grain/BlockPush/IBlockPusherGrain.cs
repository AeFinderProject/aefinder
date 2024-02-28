using AElfIndexer.Block.Dtos;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockPush;

public interface IBlockPusherGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleBlockAsync(BlockWithTransactionDto block);
    Task HandleConfirmedBlockAsync(BlockWithTransactionDto block);
    Task InitializeAsync(string pushToken, long startHeight);
}