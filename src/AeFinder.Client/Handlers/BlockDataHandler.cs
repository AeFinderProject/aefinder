using AeFinder.Block.Dtos;
using AeFinder.Client.Providers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Client.Handlers;

public abstract class BlockDataHandler: BlockChainDataHandler<BlockInfo>
{
    protected BlockDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAeFinderClientInfoProvider aefinderClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        ILogger<BlockDataHandler> logger) : base(clusterClient,
        objectMapper, aefinderClientInfoProvider, logger, dAppDataProvider,
        blockStateSetProvider, dAppDataIndexManagerProvider)
    {
    }

    public override BlockFilterType FilterType => BlockFilterType.Block;

    protected override async Task ProcessDataAsync(List<BlockInfo> data)
    {
        try
        {
            await ProcessBlocksAsync(data);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Process Client Blocks Error!" + e.Message);
        }
    }

    protected override List<BlockInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return new List<BlockInfo> { ObjectMapper.Map<BlockWithTransactionDto, BlockInfo>(blockDto) };
    }

    protected abstract Task ProcessBlocksAsync(List<BlockInfo> data);
}