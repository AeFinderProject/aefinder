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

public class BlockHandler:IDistributedEventHandler<NewBlockEto>,ITransientDependency
{
    private readonly INESTRepository<BlockTest, Guid> _testRepository;
    private readonly INESTRepository<Block, Guid> _nestRepository;
    private readonly ILogger<BlockHandler> _logger;

    public BlockHandler(
        INESTRepository<BlockTest, Guid> testRepository,
        INESTRepository<Block,Guid> nestRepository,
        ILogger<BlockHandler> logger)
    {
        _testRepository = testRepository;
        _nestRepository = nestRepository;
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
        await _nestRepository.AddAsync(eventData);
    }
}