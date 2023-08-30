using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockDataHandler: BlockChainDataHandler<BlockInfo>
{
    protected BlockDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        ILogger<BlockDataHandler> logger) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider, logger, dAppDataProvider,
        blockStateSetProvider, dAppDataIndexManagerProvider)
    {
    }

    public override BlockFilterType FilterType => BlockFilterType.Block;

    protected override async Task ProcessDataAsync(List<BlockInfo> data)
    {
        foreach (var blockInfo in data)
        {
            try
            {
                await ProcessBlockAsync(blockInfo);
            }
            catch (Exception e)
            {
                throw new DAppHandlingException(
                    $"Handle Block Error! ChainId: {blockInfo.ChainId} BlockHeight: {blockInfo.BlockHeight} BlockHash: {blockInfo.BlockHash}.",
                    e);
            }
        }
    }

    protected override List<BlockInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return new List<BlockInfo> { ObjectMapper.Map<BlockWithTransactionDto, BlockInfo>(blockDto) };
    }

    protected abstract Task ProcessBlockAsync(BlockInfo block);
}