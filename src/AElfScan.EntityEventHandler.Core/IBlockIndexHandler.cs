using System.Runtime.Serialization;
using AElfScan.Block.Dtos;
using AElfScan.Entities.Es;
using AElfScan.Etos;
using AElfScan.Grains.Grain.BlockScan;
using AElfScan.Grains.Grain.Chains;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfScan;

public interface IBlockIndexHandler
{
    Task ProcessNewBlockAsync(BlockIndex block);
    Task ProcessConfirmedBlocksAsync(List<BlockIndex> confirmBlocks);
}

public class BlockIndexHandler : IBlockIndexHandler, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    
    public ILogger<BlockIndexHandler> Logger { get; set; }

    public BlockIndexHandler(IObjectMapper objectMapper, IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        
        Logger = NullLogger<BlockIndexHandler>.Instance;
    }

    public async Task ProcessNewBlockAsync(BlockIndex block)
    {
        try
        {
            var chainGrain = _clusterClient.GetGrain<IChainGrain>(block.ChainId);
            await chainGrain.SetLatestBlockAsync(block.BlockHash, block.BlockNumber);
            
            var dto = _objectMapper.Map<BlockIndex, BlockDto>(block);

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
                    await blockScanGrain.HandleNewBlockAsync(dto);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,$"Process new block failed.");
            throw;
        }
    }

    public async Task ProcessConfirmedBlocksAsync(List<BlockIndex> confirmBlocks)
    {
        try
        {
            var chainId = confirmBlocks.First().ChainId;

            var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
            await chainGrain.SetLatestConfirmBlockAsync(confirmBlocks.Last().BlockHash,
                confirmBlocks.Last().BlockNumber);

            var dtos = _objectMapper.Map<List<BlockIndex>, List<BlockDto>>(confirmBlocks);

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
                    await blockScanGrain.HandleConfirmedBlockAsync(dtos);
                }
            });

            await tasks.WhenAll();
        }
        catch (Exception e)
        {
            Logger.LogError(e,$"Process Confirmed block failed.");
            throw;
        }
    }
}