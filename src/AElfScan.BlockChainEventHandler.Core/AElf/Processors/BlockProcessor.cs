using AElf.Contracts.Consensus.AEDPoS;
using AElfScan.AElf.ETOs;
using AElfScan.EventData;
using AElfScan.Grain;
using AElfScan.Orleans;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.AElf.Processors;

public class BlockProcessor:IDistributedEventHandler<BlockChainDataEto>,ITransientDependency
{
    private readonly IOrleansClusterClientFactory _clusterClientFactory;
    private readonly ILogger<BlockProcessor> _logger;

    public BlockProcessor(IOrleansClusterClientFactory clusterClientFactory,
        ILogger<BlockProcessor> logger)
    {
        _clusterClientFactory = clusterClientFactory;
        _logger = logger;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        _logger.LogInformation("Start connect to Silo Server....");
        using (var client = await _clusterClientFactory.GetClient())
        {
            _logger.LogInformation("Prepare Grain Class，While the client IsInitialized:" + client.IsInitialized);
            var blockGrain = client.GetGrain<IBlockGrain>(4);
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
                                    _logger.LogInformation($"IrreversibleBlockFound: {libFound.IrreversibleBlockHeight}");
                                }
                            }
                        }
                    }
                }
                // blockEvent.LibBlockHash = "";
                // blockEvent.LibBlockNumber = 0;


                // var eventData = ObjectMapper.Map<BlockEventDataDto, BlockEventData>(eventDataDto);
                _logger.LogInformation("Start Raise Event of Block Number:" + blockEvent.BlockNumber);
                // await blockGrain.NewEvent(eventData);
                await blockGrain.SaveBlock(blockEvent);
            }
        }

        _logger.LogInformation("Stop connect to Silo Server");
        
        
        await Task.CompletedTask;
    }
}