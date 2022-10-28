using System.Runtime.Serialization;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Etos;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Nito.AsyncEx;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Block = AElfScan.AElf.Entities.Es.Block;

namespace AElfScan;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(Block block);
    Task ProcessConfirmBlocksAsync(List<Block> confirmBlocks);
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

    public async Task ProcessNewBlockAsync(Block block)
    {
        var client = _clusterClientAppService.Client;

        var chainGrain = client.GetGrain<IChainGrain>(block.ChainId);
        await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockNumber);

        var clientManagerGrain = client.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsAsync(block.ChainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var id = block.ChainId + clientId;
            var clientGrain = client.GetGrain<IClientGrain>(id);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= block.BlockNumber)
            {
                var blockScanGrain = client.GetGrain<IBlockScanGrain>(id);
                var dto = _objectMapper.Map<Block, BlockDto>(block);
                await blockScanGrain.HandleNewBlockAsync(dto);
            }
        });

        await tasks.WhenAll();
    }

    public async Task ProcessConfirmBlocksAsync(List<Block> confirmBlocks)
    {
        var client = _clusterClientAppService.Client;
        var chainId = confirmBlocks.First().ChainId;

        var chainGrain = client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
            confirmBlocks.Last().BlockNumber);

        var clientManagerGrain = client.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsAsync(chainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var id = chainId + clientId;
            var clientGrain = client.GetGrain<IClientGrain>(id);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= confirmBlocks.First().BlockNumber)
            {
                var blockScanGrain = client.GetGrain<IBlockScanGrain>(id);
                var dtos = _objectMapper.Map<List<Block>, List<BlockDto>>(confirmBlocks);
                await blockScanGrain.HandleConfirmedBlockAsync(dtos);
            }
        });

        await tasks.WhenAll();
    }
}