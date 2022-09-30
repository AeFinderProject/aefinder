using AElf.Contracts.Consensus.AEDPoS;
using AElfScan.AElf.DTOs;
using AElfScan.AElf.Etos;
using AElfScan.EventData;
using AElfScan.Grain;
using AElfScan.State;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElfScan.AElf.Processors;

public class BlockChainDataEventHandler : IDistributedEventHandler<BlockChainDataEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<BlockChainDataEventHandler> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    

    public BlockChainDataEventHandler(
        IClusterClient clusterClient,
        ILogger<BlockChainDataEventHandler> logger,
        IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        _logger.LogInformation("Start connect to a Silo Server to get a grain");
        var blockGrain = _clusterClient.GetGrain<IBlockGrain>(45);
        foreach (var blockItem in eventData.Blocks)
        {
            BlockEventData blockEvent = new BlockEventData();
            blockEvent.ChainId = eventData.ChainId;
            blockEvent.BlockHash = blockItem.BlockHash;
            blockEvent.BlockNumber = blockItem.BlockNumber;
            blockEvent.PreviousBlockHash = blockItem.PreviousBlockHash;
            blockEvent.BlockTime = blockItem.BlockTime;

            //analysis lib found event content
            var libLogEvent = blockItem.Transactions?.SelectMany(t => t.LogEvents)
                .FirstOrDefault(e => e.EventName == "IrreversibleBlockFound");
            if (libLogEvent != null)
            {
                blockEvent.LibBlockNumber = AnalysisBlockLibFoundEvent(libLogEvent.ExtraProperties["Indexed"]);
            }


            _logger.LogInformation("Save Block Number:" + blockEvent.BlockNumber);
            List<Block> libBlockList = await blockGrain.SaveBlock(blockEvent);
            _logger.LogInformation($"libBlockList:{libBlockList}");
            if (libBlockList != null)
            {
                //publish new block event
                await _distributedEventBus.PublishAsync(ConvertToNewBlockEto(blockItem,eventData.ChainId));
                _logger.LogInformation($"NewBlock Event is already published:{blockItem.BlockHash}");

                //publish confirm blocks event
                _logger.LogInformation("libBlockList length:" + libBlockList.Count);
                if (libBlockList.Count > 0)
                {
                    var confirmBlockList =
                        _objectMapper.Map<List<AElfScan.State.Block>, List<ConfirmBlockEto>>(libBlockList);
                    await _distributedEventBus.PublishAsync(new ConfirmBlocksEto()
                        { ConfirmBlocks = confirmBlockList });
                }
            }
        }

        _logger.LogInformation("Stop connect to Silo Server");

        await Task.CompletedTask;
    }

    private long AnalysisBlockLibFoundEvent(string logEventIndexed)
    {
        List<string> IndexedList =
            JsonConvert.DeserializeObject<List<string>>(logEventIndexed);
        var libFound = new IrreversibleBlockFound();
        libFound.MergeFrom(ByteString.FromBase64(IndexedList[0]));
        _logger.LogInformation(
            $"IrreversibleBlockFound: {libFound}");
        return libFound.IrreversibleBlockHeight;
    }

    private NewBlockEto ConvertToNewBlockEto(BlockEto blockItem,string chainId)
    {
        NewBlockEto newBlock = _objectMapper.Map<BlockEto, NewBlockEto>(blockItem);
        newBlock.Id = Guid.NewGuid();
        // newBlock.Id = newBlock.BlockHash;
        newBlock.ChainId = chainId;
        newBlock.IsConfirmed = false;
        foreach (var transaction in newBlock.Transactions)
        {
            transaction.ChainId = chainId;
            transaction.BlockHash = newBlock.BlockHash;
            transaction.BlockNumber = newBlock.BlockNumber;
            transaction.BlockTime = newBlock.BlockTime;
            transaction.IsConfirmed = false;

            foreach (var logEvent in transaction.LogEvents)
            {
                logEvent.ChainId = chainId;
                logEvent.BlockHash = newBlock.BlockHash;
                logEvent.BlockNumber = newBlock.BlockNumber;
                logEvent.BlockTime = newBlock.BlockTime;
                logEvent.IsConfirmed = false;
                logEvent.TransactionId = transaction.TransactionId;
            }
        }

        return newBlock;
    }
}