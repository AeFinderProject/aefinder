using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.AElf;

public class BlockHandler:IDistributedEventHandler<NewBlockEto>,
    IDistributedEventHandler<ConfirmBlockEto>,ITransientDependency
{
    private readonly INESTRepository<BlockTest, Guid> _testRepository;
    private readonly INESTRepository<Block, string> _blockIndexRepository;
    private readonly ILogger<BlockHandler> _logger;

    public BlockHandler(
        INESTRepository<BlockTest, Guid> testRepository,
        INESTRepository<Block,string> blockIndexRepository,
        ILogger<BlockHandler> logger)
    {
        _testRepository = testRepository;
        _blockIndexRepository = blockIndexRepository;
        _logger = logger;
    }
    
    // public async Task HandleEventAsync(BlockTestEto eventData)
    // {
    //     var block = eventData;
    //     _logger.LogInformation($"test block is adding, id: {block.Id}  , BlockNumber: {block.BlockNumber} , IsConfirmed: {block.IsConfirmed}");
    //     await _testRepository.AddAsync(eventData);
    // }

    public async Task HandleEventAsync(NewBlockEto eventData)
    {
        var block = eventData;
        _logger.LogInformation($"test block is adding, id: {block.Id}  , BlockNumber: {block.BlockNumber} , IsConfirmed: {block.IsConfirmed}");
        var blockIndex = await _blockIndexRepository.GetAsync(eventData.BlockHash);
        if (blockIndex != null)
        {
            _logger.LogInformation($"block already exist-{blockIndex}, Add failure!");
        }
        else
        {
            await _blockIndexRepository.AddAsync(eventData);
        }
        
    }

    public async Task HandleEventAsync(ConfirmBlockEto eventData)
    {
        _logger.LogInformation($"block:{eventData.BlockNumber} is confirming");
        var blockIndex = await _blockIndexRepository.GetAsync(eventData.BlockHash);
        if (blockIndex != null)
        {
            blockIndex.IsConfirmed = true;
            foreach (var transaction in blockIndex.Transactions)
            {
                transaction.IsConfirmed = true;
                foreach (var logEvent in transaction.LogEvents)
                {
                    logEvent.IsConfirmed = true;
                }
            }

            await _blockIndexRepository.UpdateAsync(blockIndex);
        }
        else
        {
            _logger.LogInformation($"Confirm failure,block{eventData.BlockHash} is not exist!");
        }
    }
}