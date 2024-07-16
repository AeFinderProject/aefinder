using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.BlockSync;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BlockSyncAppService : ApplicationService, IBlockSyncAppService
{
    private readonly BlockSyncOptions _blockSyncOptions;

    public BlockSyncAppService(IOptionsSnapshot<BlockSyncOptions> blockSyncOptions)
    {
        _blockSyncOptions = blockSyncOptions.Value;
    }

    public Task<BlockSyncMode> GetBlockSyncModeAsync(string chainId, long blockHeight)
    {
        if (_blockSyncOptions.FastSyncEndHeight.TryGetValue(chainId, out var endHeight) && blockHeight <= endHeight)
        {
            return Task.FromResult(BlockSyncMode.FastSyncMode);
        }

        return Task.FromResult(BlockSyncMode.NormalMode);
    }
}