using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.BlockChainEventHandler.DTOs;
using AeFinder.BlockChainEventHandler.Providers;
using AeFinder.BlockSync;
using AeFinder.Etos;
using AeFinder.Grains.EventData;
using AeFinder.Metrics;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.OpenTelemetry.ExecutionTime;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.BlockChainEventHandler.Processors;

[AggregateExecutionTime]
public class BlockChainDataEventHandler : IDistributedEventHandler<BlockChainDataEto>, ITransientDependency
{
    private readonly ILogger<BlockChainDataEventHandler> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockGrainProvider _blockGrainProvider;
    private readonly IBlockSyncAppService _blockSyncAppService;
    private readonly IElapsedTimeRecorder _elapsedTimeRecorder;

    public BlockChainDataEventHandler(
        ILogger<BlockChainDataEventHandler> logger,
        IObjectMapper objectMapper,
        IBlockGrainProvider blockGrainProvider,
        IDistributedEventBus distributedEventBus, IBlockSyncAppService blockSyncAppService, IElapsedTimeRecorder elapsedTimeRecorder)
    {
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _blockSyncAppService = blockSyncAppService;
        _elapsedTimeRecorder = elapsedTimeRecorder;
        _objectMapper = objectMapper;
        _blockGrainProvider = blockGrainProvider;
    }

    public virtual async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        var firstBlock = eventData.Blocks.First();
        _elapsedTimeRecorder.Record("ReceiveBlockChainData",
            (long)(DateTime.UtcNow - firstBlock.BlockTime).TotalMilliseconds);
        var lastBlockHeight = eventData.Blocks.Last().BlockHeight;
        var syncMode = await _blockSyncAppService.GetBlockSyncModeAsync(eventData.ChainId, lastBlockHeight);

        _logger.LogInformation(
            "Received BlockChainDataEto form {ChainId}, start block: {StartBlockHeight}, end block: {EndBlockHeight}, sync mode: {SyncMode}",
            eventData.ChainId, firstBlock.BlockHeight, lastBlockHeight, syncMode);

        //prepare data
        List<Task<NewBlockTaskEntity>> prepareDataTaskList = new List<Task<NewBlockTaskEntity>>();
        List<NewBlockEto> newBlockEtos = new List<NewBlockEto>();
        List<BlockData> blockEventDatas = new List<BlockData>();
        foreach (var blockItem in eventData.Blocks)
        {
            Func<NewBlockTaskEntity> funcBlockConvertTask = () =>
            {
                return ConvertBlockDataTask(blockItem, eventData.ChainId);
            };
            Task<NewBlockTaskEntity> task = Task.Run(funcBlockConvertTask);
            prepareDataTaskList.Add(task);
        }

        await Task.WhenAll(prepareDataTaskList.ToArray());

        foreach (var task in prepareDataTaskList)
        {
            var newBlockTaskEntity = task.Result;
            newBlockEtos.Add(newBlockTaskEntity.newBlockEto);
            blockEventDatas.Add(newBlockTaskEntity.BlockData);
        }

        var blockBranchGrain = await _blockGrainProvider.GetBlockBranchGrain(eventData.ChainId);
        List<BlockData> libBlockList = await blockBranchGrain.SaveBlocks(blockEventDatas);

        if (libBlockList != null)
        {
            if (syncMode == BlockSyncMode.NormalMode)
            {
                await _distributedEventBus.PublishAsync(new NewBlocksEto()
                    { NewBlocks = newBlockEtos });
            }

            if (libBlockList.Count > 0)
            {
                libBlockList = libBlockList.OrderBy(b => b.BlockHeight).ToList();

                //publish confirm blocks event
                var confirmBlockList =
                    _objectMapper.Map<List<BlockData>, List<ConfirmBlockEto>>(libBlockList);
                await _distributedEventBus.PublishAsync(new ConfirmBlocksEto()
                    { ConfirmBlocks = confirmBlockList });
                
                //clear block grains data
                await ClearBlockGrainsAsync(libBlockList);
            }
        }


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
        newBlock.Confirmed = false;
        foreach (var transaction in newBlock.Transactions)
        {
            transaction.ChainId = chainId;
            transaction.BlockHash = newBlock.BlockHash;
            transaction.PreviousBlockHash = newBlock.PreviousBlockHash;
            transaction.BlockHeight = newBlock.BlockHeight;
            transaction.BlockTime = newBlock.BlockTime;
            transaction.Confirmed = false;
        
            foreach (var logEvent in transaction.LogEvents)
            {
                logEvent.ChainId = chainId;
                logEvent.BlockHash = newBlock.BlockHash;
                logEvent.PreviousBlockHash = newBlock.PreviousBlockHash;
                logEvent.BlockHeight = newBlock.BlockHeight;
                logEvent.BlockTime = newBlock.BlockTime;
                logEvent.Confirmed = false;
                logEvent.TransactionId = transaction.TransactionId;
            }
        }

        return newBlock;
    }

    private NewBlockTaskEntity ConvertBlockDataTask(BlockEto blockItem,string chainId)
    {
        var newBlockEto = ConvertToNewBlockEto(blockItem, chainId);
        BlockData block = new BlockData();
        block = _objectMapper.Map<NewBlockEto, BlockData>(newBlockEto);

        //analysis lib found event content
        var libLogEvent = blockItem.Transactions?.SelectMany(t => t.LogEvents)
            .FirstOrDefault(e => e.EventName == "IrreversibleBlockFound");
        if (libLogEvent != null)
        {
            block.LibBlockHeight = AnalysisBlockLibFoundEvent(libLogEvent.ExtraProperties["Indexed"]);
        }

        NewBlockTaskEntity resultEntity = new NewBlockTaskEntity();
        resultEntity.newBlockEto = newBlockEto;
        resultEntity.BlockData = block;

        return resultEntity;
    }
    
    private async Task ClearBlockGrainsAsync(List<BlockData> blockDatas)
    {
        var deleteBlockGrainTaskList = new List<Task>();
        foreach (var blockData in blockDatas)
        {
            var blockGrain = await _blockGrainProvider.GetBlockGrain(blockData.ChainId, blockData.BlockHash);
            deleteBlockGrainTaskList.Add(blockGrain.DeleteGrainStateAsync());
        }

        await Task.WhenAll(deleteBlockGrainTaskList);
    }
}