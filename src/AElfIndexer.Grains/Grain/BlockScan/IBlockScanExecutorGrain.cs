using AElfIndexer.Block.Dtos;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockScanExecutorGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleBlockAsync(BlockWithTransactionDto block);
    Task HandleConfirmedBlockAsync(BlockWithTransactionDto block);
    Task InitializeAsync(string scanToken, long startHeight);
}