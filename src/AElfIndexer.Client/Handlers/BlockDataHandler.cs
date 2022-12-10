using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockDataHandler<T> : BlockChainDataHandler<BlockInfo,T>
{
    protected BlockDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider)
    {
    }

    public override BlockFilterType FilterType => BlockFilterType.Block;

    protected override List<BlockInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return new List<BlockInfo> { ObjectMapper.Map<BlockWithTransactionDto, BlockInfo>(blockDto) };
    }
}