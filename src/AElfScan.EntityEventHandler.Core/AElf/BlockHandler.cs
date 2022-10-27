using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Index = System.Index;

namespace AElfScan.AElf;

public class BlockHandler:IDistributedEventHandler<NewBlockEto>,
    IDistributedEventHandler<ConfirmBlocksEto>,ITransientDependency
{
    private readonly INESTRepository<Block, string> _blockIndexRepository;
    private readonly ILogger<BlockHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockIndexHandler _blockIndexHandler;

    public BlockHandler(
        INESTRepository<Block,string> blockIndexRepository,
        ILogger<BlockHandler> logger,
        IObjectMapper objectMapper, IBlockIndexHandler blockIndexHandler)
    {
        _blockIndexRepository = blockIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockIndexHandler = blockIndexHandler;
    }

    public async Task HandleEventAsync(NewBlockEto eventData)
    {
        var block = eventData;
        _logger.LogInformation($"block is adding, id: {block.BlockHash}  , BlockNumber: {block.BlockNumber} , IsConfirmed: {block.IsConfirmed}");
        var blockIndex = await _blockIndexRepository.GetAsync(q=>
            q.Term(i=>i.Field(f=>f.BlockHash).Value(eventData.BlockHash)));
        if (blockIndex != null)
        {
            _logger.LogInformation($"block already exist-{blockIndex}, Add failure!");
        }
        else
        {
            await _blockIndexRepository.AddAsync(eventData);
            await _blockIndexHandler.ProcessNewBlockAsync(eventData);
        }
        
    }

    public async Task HandleEventAsync(ConfirmBlocksEto eventData)
    {
        var indexes = new List<Block>();
        foreach (var confirmBlock in eventData.ConfirmBlocks)
        {
            _logger.LogInformation($"block:{confirmBlock.BlockNumber} is confirming");
            var blockIndex = _objectMapper.Map<ConfirmBlockEto, Block>(confirmBlock);
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
            indexes.Add(blockIndex);

            //find the same height blocks
            var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.BlockNumber).Value(confirmBlock.BlockNumber)));
            QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Bool(b => b.Must(mustQuery));

            var forkBlockList = await _blockIndexRepository.GetListAsync(Filter);
            if (forkBlockList.Item1 == 0)
            {
                continue;
            }

            //delete the same height fork block
            foreach (var forkBlock in forkBlockList.Item2)
            {
                if (forkBlock.BlockHash == confirmBlock.BlockHash)
                {
                    continue;
                }

                await _blockIndexRepository.DeleteAsync(forkBlock);
                _logger.LogInformation($"block {forkBlock.BlockHash} has been deleted.");
            }
        }

        await _blockIndexHandler.ProcessConfirmBlocksAsync(indexes);
    }
}