using AElfIndexer.Block.Dtos;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class BlockDataHandler<T> : BlockChainDataHandler<BlockInfo,T>
{
    protected BlockDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper) : base(clusterClient,
        objectMapper)
    {
    }

    public override BlockFilterType FilterType => BlockFilterType.Block;

    protected override List<BlockInfo> GetData(BlockDto blockDto)
    {
        return new List<BlockInfo> { ObjectMapper.Map<BlockDto, BlockInfo>(blockDto) };
    }
}