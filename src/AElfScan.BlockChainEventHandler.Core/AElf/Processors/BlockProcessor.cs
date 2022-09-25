using AElf.Contracts.Consensus.AEDPoS;
using AElfScan.AElf.ETOs;
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

namespace AElfScan.AElf.Processors;

public class BlockProcessor : IDistributedEventHandler<BlockChainDataEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<BlockProcessor> _logger;
    private readonly IDistributedEventBus _distributedEventBus;

    public BlockProcessor(
        IClusterClient clusterClient,
        ILogger<BlockProcessor> logger,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        _logger.LogInformation("Start connect to Silo Server....");
        _logger.LogInformation("Prepare Grain Class，While the client IsInitialized:" + _clusterClient.IsInitialized);

        var blockGrain = _clusterClient.GetGrain<IBlockGrain>(10);
        foreach (var blockItem in eventData.Blocks)
        {
            BlockEventData blockEvent = new BlockEventData();
            blockEvent.ChainId = eventData.ChainId;
            blockEvent.BlockHash = blockItem.BlockHash;
            blockEvent.BlockNumber = blockItem.BlockNumber;
            blockEvent.PreviousBlockHash = blockItem.PreviousBlockHash;
            blockEvent.BlockTime = blockItem.BlockTime;

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
            if (libBlockList == null)
            {
                //means block has been ignored
            }
            else
            {
                //Todo: new block event
                //     _logger.LogInformation("Start publish Event to Rabbitmq");
                //     await _distributedEventBus.PublishAsync(
                //         new BlockTestEto
                //         {
                //             Id = Guid.NewGuid(),
                //             BlockNumber = eventDataDto.BlockNumber,
                //             BlockTime = DateTime.Now,
                //             IsConfirmed = eventDataDto.IsConfirmed
                //         }
                //         );
                //     _logger.LogInformation("Test Block Event is already published");

                //Todo: confirm blocks event
                foreach (var libBlock in libBlockList)
                {
                    //     _logger.LogInformation("Start publish Event to Rabbitmq");
                    //     await _distributedEventBus.PublishAsync(
                    //         new BlockTestEto
                    //         {
                    //             Id = Guid.NewGuid(),
                    //             BlockNumber = eventDataDto.BlockNumber,
                    //             BlockTime = DateTime.Now,
                    //             IsConfirmed = eventDataDto.IsConfirmed
                    //         }
                    //         );
                    //     _logger.LogInformation("Test Block Event is already published");
                }
            }
        }

        _logger.LogInformation("Stop connect to Silo Server");

        await Task.CompletedTask;
    }
}