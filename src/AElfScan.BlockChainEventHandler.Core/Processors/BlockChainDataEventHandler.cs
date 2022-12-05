using AElf.Contracts.Consensus.AEDPoS;
using AElfScan.DTOs;
using AElfScan.Etos;
using AElfScan.Grains.EventData;
using AElfScan.Grains.Grain.Blocks;
using AElfScan.Providers;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AElfScan.Processors;

public class BlockChainDataEventHandler : IDistributedEventHandler<BlockChainDataEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<BlockChainDataEventHandler> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockGrainProvider _blockGrainProvider;
    private readonly BlockChainEventHandlerOptions _blockChainEventHandlerOptions;

    public BlockChainDataEventHandler(
        IClusterClient clusterClient,
        ILogger<BlockChainDataEventHandler> logger,
        IObjectMapper objectMapper,
        IBlockGrainProvider blockGrainProvider,
        IOptionsSnapshot<BlockChainEventHandlerOptions> blockChainEventHandlerOptions,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _blockChainEventHandlerOptions = blockChainEventHandlerOptions.Value;
        _blockGrainProvider = blockGrainProvider;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        _logger.LogInformation($"Received BlockChainDataEto form {eventData.ChainId}, start block: {eventData.Blocks.First().BlockNumber}, end block: {eventData.Blocks.Last().BlockNumber},");
        // var blockGrain = _clusterClient.GetGrain<IBlockGrain>(_orleansClientOption.AElfBlockGrainPrimaryKey);
        var blockGrain = await _blockGrainProvider.GetBlockGrain(eventData.ChainId);
        int processedBlockCount = 0;

        List<Task<NewBlockTaskEntity>> taskList = new List<Task<NewBlockTaskEntity>>();
        List<NewBlockEto> newBlockEtos = new List<NewBlockEto>();
        List<BlockEventData> blockEventDatas = new List<BlockEventData>();
        foreach (var blockItem in eventData.Blocks)
        {
            Func<NewBlockTaskEntity> funcBlockConvertTask = () =>
            {
                return ConvertBlockDataTask(blockItem, eventData.ChainId);
            };
            Task<NewBlockTaskEntity> task = Task.Run(funcBlockConvertTask);
            taskList.Add(task);
        }

        await Task.WhenAll(taskList.ToArray());

        foreach (var task in taskList)
        {
            var newBlockTaskEntity = task.Result;
            newBlockEtos.Add(newBlockTaskEntity.newBlockEto);
            blockEventDatas.Add(newBlockTaskEntity.blockEventData);
        }

        int blockLimit = _blockChainEventHandlerOptions.BlockPartionLimit;
        int partion = (eventData.Blocks.Count % blockLimit) == 0
            ? (eventData.Blocks.Count / blockLimit)
            : (eventData.Blocks.Count / blockLimit) + 1;
        
        _logger.LogInformation($"blockLimit:{blockLimit} partion:{partion} ");

        for (var i = 0; i < partion; i++)
        {
            _logger.LogInformation("skip: "+(i*blockLimit).ToString());
            List<BlockEventData> libBlockList = await blockGrain.SaveBlocks(blockEventDatas.Skip(i*blockLimit).Take(blockLimit).ToList());

            if (libBlockList != null)
            {
                var newBlockPartEtos = newBlockEtos.Skip(i * blockLimit).Take(blockLimit).ToList();
                _logger.LogInformation("newBlockPartEtos count: " + newBlockPartEtos.Count);
                //publish new block event
                await _distributedEventBus.PublishAsync(new NewBlocksEto()
                    { NewBlocks = newBlockPartEtos });

                processedBlockCount = processedBlockCount + newBlockPartEtos.Count;

                if (libBlockList.Count > 0)
                {
                    libBlockList = libBlockList.OrderBy(b => b.BlockNumber).ToList();
                    //publish confirm blocks event
                    var confirmBlockList =
                        _objectMapper.Map<List<BlockEventData>, List<ConfirmBlockEto>>(libBlockList);
                    await _distributedEventBus.PublishAsync(new ConfirmBlocksEto()
                        { ConfirmBlocks = confirmBlockList });
                }
            }
        }

        var primaryKeyGrain = _clusterClient.GetGrain<IPrimaryKeyGrain>(eventData.ChainId + AElfScanConsts.PrimaryKeyGrainIdSuffix);
        await primaryKeyGrain.SetCounter(processedBlockCount);
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
        // newBlock.Id = Guid.NewGuid();
        newBlock.Id = newBlock.BlockHash;
        newBlock.ChainId = chainId;
        newBlock.IsConfirmed = false;
        foreach (var transaction in newBlock.Transactions)
        {
            transaction.ChainId = chainId;
            transaction.BlockHash = newBlock.BlockHash;
            transaction.PreviousBlockHash = newBlock.PreviousBlockHash;
            transaction.BlockNumber = newBlock.BlockNumber;
            transaction.BlockTime = newBlock.BlockTime;
            transaction.IsConfirmed = false;

            foreach (var logEvent in transaction.LogEvents)
            {
                logEvent.ChainId = chainId;
                logEvent.BlockHash = newBlock.BlockHash;
                logEvent.PreviousBlockHash = newBlock.PreviousBlockHash;
                logEvent.BlockNumber = newBlock.BlockNumber;
                logEvent.BlockTime = newBlock.BlockTime;
                logEvent.IsConfirmed = false;
                logEvent.TransactionId = transaction.TransactionId;
            }
        }

        return newBlock;
    }

    private NewBlockTaskEntity ConvertBlockDataTask(BlockEto blockItem,string chainId)
    {
        var newBlockEto = ConvertToNewBlockEto(blockItem, chainId);
        BlockEventData blockEvent = new BlockEventData();
        blockEvent = _objectMapper.Map<NewBlockEto, BlockEventData>(newBlockEto);

        //analysis lib found event content
        var libLogEvent = blockItem.Transactions?.SelectMany(t => t.LogEvents)
            .FirstOrDefault(e => e.EventName == "IrreversibleBlockFound");
        if (libLogEvent != null)
        {
            blockEvent.LibBlockNumber = AnalysisBlockLibFoundEvent(libLogEvent.ExtraProperties["Indexed"]);
        }

        NewBlockTaskEntity resultEntity = new NewBlockTaskEntity();
        resultEntity.newBlockEto = newBlockEto;
        resultEntity.blockEventData = blockEvent;

        return resultEntity;
    }
}