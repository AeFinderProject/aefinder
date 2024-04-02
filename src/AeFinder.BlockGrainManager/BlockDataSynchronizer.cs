using AeFinder.Block;
using AeFinder.Block.Dtos;
using AeFinder.BlockChainEventHandler.DTOs;
using AeFinder.BlockChainEventHandler.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.BlockGrainManager;

public class BlockDataSynchronizer:IBlockDataSynchronizer, ITransientDependency
{
    private readonly ILogger<BlockDataSynchronizer> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockAppService _blockAppService;
    private readonly IBlockChainDataEventProcessor _blockChainDataEventProcessor;
    private readonly BlockGrainManagerOptions _blockGrainManagerOptions;
    private int MaxSyncBlockCount = 1000;
    
    public BlockDataSynchronizer(
        ILogger<BlockDataSynchronizer> logger,
        IObjectMapper objectMapper,
        IOptionsSnapshot<BlockGrainManagerOptions> blockGrainManagerOptions,
        IBlockAppService blockAppService,
        IBlockChainDataEventProcessor blockChainDataEventProcessor)
    {
        _logger = logger;
        _blockAppService = blockAppService;
        _blockChainDataEventProcessor= blockChainDataEventProcessor;
        _objectMapper = objectMapper;
        _blockGrainManagerOptions = blockGrainManagerOptions.Value;
    }
    
    public async Task SyncConfirmedBlockAsync(string chainId, long startHeight, long syncEndHeight)
    {
        MaxSyncBlockCount = _blockGrainManagerOptions.PerSyncBlockCount > 0 ? _blockGrainManagerOptions.PerSyncBlockCount : MaxSyncBlockCount;
        while (true)
        {
            var endHeight = (startHeight + MaxSyncBlockCount - 1) > syncEndHeight
                ? syncEndHeight
                : (startHeight + MaxSyncBlockCount - 1);
            _logger.LogInformation("Syncing blocks from {0} to {1}", startHeight, endHeight);
            //Get blocks from ES 
            var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
            {
                ChainId = chainId,
                IsOnlyConfirmed = true,
                StartBlockHeight = startHeight,
                EndBlockHeight = endHeight
            });
            if (blocks.Count == 0)
            {
                _logger.LogError("Confirmed block count is 0. Block height: {0}", startHeight);
                _logger.LogInformation("Sync block stopped.");
                break;
            }

            //Get Transactions from ES
            var transactions = await _blockAppService.GetTransactionsAsync(new GetTransactionsInput()
            {
                ChainId = chainId,
                IsOnlyConfirmed = true,
                StartBlockHeight = startHeight,
                EndBlockHeight = endHeight
            });

            //Convert into BlockChainDataEto
            var blockChainDataEto = await ConvertIntoBlockChainDataEtoAsync(chainId, blocks, transactions);
            
            //Save blocks to grain
            await _blockChainDataEventProcessor.HandleEventAsync(blockChainDataEto);
            
            //Reset startHeight
            startHeight += MaxSyncBlockCount;
            if (startHeight >= syncEndHeight)
            {
                _logger.LogInformation("Sync block finished. Last block height: {0}",
                    blocks.LastOrDefault().BlockHeight);
                break;
            }
            if (blocks.Count < MaxSyncBlockCount)
            {
                _logger.LogError("Confirmed block count {0} is less than MaxSyncBlockCount {1}. Block height: {2}",
                    blocks.Count, MaxSyncBlockCount, startHeight);
                _logger.LogInformation("Sync block stopped.");
                break;
            }
                
        }
        
        return;
    }
    
    private async Task<BlockChainDataEto> ConvertIntoBlockChainDataEtoAsync(string chainId,List<BlockDto> blockDtoList, List<TransactionDto> transactionDtoList)
    {
        var blockChainDataEto = new BlockChainDataEto
        {
            ChainId = chainId,
            Blocks = new List<BlockEto>()
        };
        foreach (var blockDto in blockDtoList)
        {
            var blockEto = _objectMapper.Map<BlockDto, BlockEto>(blockDto);
            blockEto.Transactions = new List<TransactionEto>();
            var transactionDtos= transactionDtoList.Where(t => t.BlockHash == blockDto.BlockHash);
            foreach (var transactionDto in transactionDtos)
            {
                var transactionEto = _objectMapper.Map<TransactionDto, TransactionEto>(transactionDto);
                blockEto.Transactions.Add(transactionEto);
            }

            blockChainDataEto.Blocks.Add(blockEto);
        }

        return blockChainDataEto;
    }
}