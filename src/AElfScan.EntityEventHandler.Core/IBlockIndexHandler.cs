using System.Runtime.Serialization;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Nito.AsyncEx;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScan;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(BlockIndex block);
    Task ProcessConfirmBlocksAsync(List<BlockIndex> confirmBlocks);
}

public class BlockIndexHandler : IBlockIndexHandler, ITransientDependency
{
    private readonly IClusterClientAppService _clusterClientAppService;
    private readonly IObjectMapper _objectMapper;

    public BlockIndexHandler(IClusterClientAppService clusterClientAppService, IObjectMapper objectMapper)
    {
        _clusterClientAppService = clusterClientAppService;
        _objectMapper = objectMapper;
    }

    public async Task ProcessNewBlockAsync(BlockIndex block)
    {
        var client = _clusterClientAppService.Client;

        var chainGrain = client.GetGrain<IChainGrain>(block.ChainId);
        await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockNumber);

        var clientManagerGrain = client.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(block.ChainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var clientGrain = client.GetGrain<IClientGrain>(clientId);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= block.BlockNumber)
            {
                var blockScanGrain = client.GetGrain<IBlockScanGrain>(clientId);
                var dto = _objectMapper.Map<BlockIndex, BlockDto>(block);
                await blockScanGrain.HandleNewBlockAsync(dto);
            }
        });

        await tasks.WhenAll();
    }

    public async Task ProcessConfirmBlocksAsync(List<BlockIndex> confirmBlocks)
    {
        var client = _clusterClientAppService.Client;
        var chainId = confirmBlocks.First().ChainId;

        var chainGrain = client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
            confirmBlocks.Last().BlockNumber);

        var clientManagerGrain = client.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(chainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var clientGrain = client.GetGrain<IClientGrain>(clientId);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= confirmBlocks.First().BlockNumber)
            {
                var blockScanGrain = client.GetGrain<IBlockScanGrain>(clientId);
                var dtos = _objectMapper.Map<List<BlockIndex>, List<BlockDto>>(confirmBlocks);
                await blockScanGrain.HandleConfirmedBlockAsync(dtos);
            }
        });

        await tasks.WhenAll();
    }
}