using System.Runtime.Serialization;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Nito.AsyncEx;
using Orleans;
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
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;

    public BlockIndexHandler(IObjectMapper objectMapper, IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
    }

    public async Task ProcessNewBlockAsync(BlockIndex block)
    {
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(block.ChainId);
        await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockNumber);

        var clientManagerGrain = _clusterClient.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(block.ChainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= block.BlockNumber)
            {
                var blockScanGrain = _clusterClient.GetGrain<IBlockScanGrain>(clientId);
                var dto = _objectMapper.Map<BlockIndex, BlockDto>(block);
                await blockScanGrain.HandleNewBlockAsync(dto);
            }
        });

        await tasks.WhenAll();
    }

    public async Task ProcessConfirmBlocksAsync(List<BlockIndex> confirmBlocks)
    {
        var chainId = confirmBlocks.First().ChainId;

        var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
            confirmBlocks.Last().BlockNumber);

        var clientManagerGrain = _clusterClient.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsByChainAsync(chainId);
        var tasks = clientIds.Select(async clientId =>
        {
            var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
            var clientInfo = await clientGrain.GetClientInfoAsync();
            if (clientInfo.ScanModeInfo.ScanMode == ScanMode.NewBlock &&
                clientInfo.ScanModeInfo.ScanNewBlockStartHeight <= confirmBlocks.First().BlockNumber)
            {
                var blockScanGrain = _clusterClient.GetGrain<IBlockScanGrain>(clientId);
                var dtos = _objectMapper.Map<List<BlockIndex>, List<BlockDto>>(confirmBlocks);
                await blockScanGrain.HandleConfirmedBlockAsync(dtos);
            }
        });

        await tasks.WhenAll();
    }
}