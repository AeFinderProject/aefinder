using AElf.Contracts.Consensus.AEDPoS;
using AElfScan.AElf.DTOs;
using AElfScan.AElf.Etos;
using AElfScan.EventData;
using AElfScan.Grain;
using AElfScan.Orleans;
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
    private readonly IAbpLazyServiceProvider _lazyServiceProvider;
    private IObjectMapper ObjectMapper => _lazyServiceProvider.LazyGetService<IObjectMapper>();
    

    public BlockChainDataEventHandler(
        IClusterClient clusterClient,
        ILogger<BlockChainDataEventHandler> logger,
        IAbpLazyServiceProvider lazyServiceProvider,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _lazyServiceProvider = lazyServiceProvider;
        _distributedEventBus = distributedEventBus;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        _logger.LogInformation("Start connect to Silo Server....");
        _logger.LogInformation("Prepare Grain Class，While the client IsInitialized:" + _clusterClient.IsInitialized);

        var blockGrain = _clusterClient.GetGrain<IBlockGrain>(25);
        foreach (var blockItem in eventData.Blocks)
        {
            BlockEventData blockEvent = new BlockEventData();
            blockEvent.ChainId = eventData.ChainId;
            blockEvent.BlockHash = blockItem.BlockHash;
            blockEvent.BlockNumber = blockItem.BlockNumber;
            blockEvent.PreviousBlockHash = blockItem.PreviousBlockHash;
            blockEvent.BlockTime = blockItem.BlockTime;

            // if (blockItem.Transactions.Count > 0)
            // {
            //     var libLogEvent = blockItem.Transactions.SelectMany(t => t.LogEvents)
            //         .FirstOrDefault(e => e.EventName == "IrreversibleBlockFound");
            //     if (libLogEvent != null)
            //     {
            //         string logEventIndexed = libLogEvent.ExtraProperties["Indexed"];
            //         List<string> IndexedList =
            //             JsonConvert.DeserializeObject<List<string>>(logEventIndexed);
            //         _logger.LogInformation($"LogEvent-Indexed: {IndexedList[0]}");
            //         var libFound = new IrreversibleBlockFound();
            //         libFound.MergeFrom(ByteString.FromBase64(IndexedList[0]));
            //         _logger.LogInformation(
            //             $"IrreversibleBlockFound: {libFound}");
            //         blockEvent.LibBlockNumber = libFound.IrreversibleBlockHeight;
            //     }
            // }
            

            if (blockItem.Transactions != null && blockItem.Transactions.Count > 0)
            {
                foreach (var transactionItem in blockItem.Transactions)
                {
                    if (transactionItem.LogEvents != null && transactionItem.LogEvents.Count > 0)
                    {
                        foreach (var logEventItem in transactionItem.LogEvents)
                        {
                            if (logEventItem.EventName == "IrreversibleBlockFound")
                            {
                                string logEventIndexed = logEventItem.ExtraProperties["Indexed"];
                                List<string> IndexedList =
                                    JsonConvert.DeserializeObject<List<string>>(logEventIndexed);
                                _logger.LogInformation($"LogEvent-Indexed: {IndexedList[0]}");
                                var libFound = new IrreversibleBlockFound();
                                libFound.MergeFrom(ByteString.FromBase64(IndexedList[0]));
                                _logger.LogInformation(
                                    $"IrreversibleBlockFound: {libFound}");
                                blockEvent.LibBlockNumber = libFound.IrreversibleBlockHeight;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Save Block Number:" + blockEvent.BlockNumber);
            List<Block> libBlockList = await blockGrain.SaveBlock(blockEvent);
            _logger.LogInformation($"libBlockList:{libBlockList}");
            if (libBlockList != null)
            {
                //Todo: new block event
                _logger.LogInformation("Start publish Event to Rabbitmq");
                NewBlockEto newBlock = ObjectMapper.Map<BlockEto, NewBlockEto>(blockItem);
                // newBlock.Id = Guid.NewGuid();
                newBlock.Id = newBlock.BlockHash;
                newBlock.ChainId = eventData.ChainId;
                newBlock.IsConfirmed = false;
                foreach (var transaction in newBlock.Transactions)
                {
                    transaction.ChainId = eventData.ChainId;
                    transaction.BlockHash = newBlock.BlockHash;
                    transaction.BlockNumber = newBlock.BlockNumber;
                    transaction.BlockTime = newBlock.BlockTime;
                    transaction.IsConfirmed = false;

                    foreach (var logEvent in transaction.LogEvents)
                    {
                        logEvent.ChainId = eventData.ChainId;
                        logEvent.BlockHash = newBlock.BlockHash;
                        logEvent.BlockNumber = newBlock.BlockNumber;
                        logEvent.BlockTime = newBlock.BlockTime;
                        logEvent.IsConfirmed = false;
                        logEvent.TransactionId = transaction.TransactionId;
                    }
                }
                await _distributedEventBus.PublishAsync(newBlock);
                _logger.LogInformation($"NewBlock Event is already published:{newBlock.Id}");

                //Todo: confirm blocks event
                _logger.LogInformation("libBlockList length:" + libBlockList.Count);

                if (libBlockList.Count > 0)
                {
                    var confirmBlockList = ObjectMapper.Map<List<AElfScan.State.Block>, List<ConfirmBlockEto>>(libBlockList);
                    await _distributedEventBus.PublishAsync(new ConfirmBlocksEto()
                        { ConfirmBlocks = confirmBlockList });
                }
            }
        }

        _logger.LogInformation("Stop connect to Silo Server");

        await Task.CompletedTask;
    }
}