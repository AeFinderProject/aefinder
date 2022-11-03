using AElfScan.AElf.Dtos;
using Orleans;

namespace AElfScan.Grains.Grain.BlockScan;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleNewBlockAsync(BlockDto block);
    Task HandleConfirmedBlockAsync(List<BlockDto> blocks);
    Task<Guid> InitializeAsync(string chainId, string clientId, string version);
}