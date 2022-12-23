using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockDataHandler<T> : BlockChainDataHandler<BlockInfo,T>
{
    protected BlockDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider,
        ILogger<BlockDataHandler<T>> logger) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider, logger)
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
            Logger.LogError(e, e.Message);
        }
    }

    protected override List<BlockInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return new List<BlockInfo> { ObjectMapper.Map<BlockWithTransactionDto, BlockInfo>(blockDto) };
    }

    protected abstract Task ProcessBlocksAsync(List<BlockInfo> data);
}