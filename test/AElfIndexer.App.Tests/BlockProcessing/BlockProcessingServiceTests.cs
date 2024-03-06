using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.App.BlockState;
using AElfIndexer.App.Handlers;
using AElfIndexer.App.MockPlugin;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Sdk;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElfIndexer.App.BlockProcessing;

public class BlockProcessingServiceTests: AElfIndexerAppTestBase
{
    private readonly IBlockProcessingService _blockProcessingService;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IReadOnlyRepository<BlockEntity> _blockEntityRepository;
    private readonly IReadOnlyRepository<TransactionEntity> _transactionEntityRepository;
    private readonly IReadOnlyRepository<TransferEntity> _transferEntityRepository;
    private readonly IReadOnlyRepository<AccountBalanceEntity> _accountBalanceEntityRepository;
    private readonly ILocalEventBus _localEventBus;

    public BlockProcessingServiceTests()
    {
        _blockEntityRepository = GetRequiredService<IReadOnlyRepository<BlockEntity>>();
        _transactionEntityRepository = GetRequiredService<IReadOnlyRepository<TransactionEntity>>();
        _transferEntityRepository = GetRequiredService<IReadOnlyRepository<TransferEntity>>();
        _accountBalanceEntityRepository = GetRequiredService<IReadOnlyRepository<AccountBalanceEntity>>();
        _blockProcessingService = GetRequiredService<IBlockProcessingService>();
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _appStateProvider = GetRequiredService<IAppStateProvider>();
        _localEventBus = GetRequiredService<ILocalEventBus>();
    }

    [Fact]
    public async Task Process_Test()
    {
        var chainId = "AELF";
        LastIrreversibleBlockStateSetFoundEventData lastIrreversibleBlockStateSetFoundEventData = null;

        _localEventBus.Subscribe<LastIrreversibleBlockStateSetFoundEventData>(d =>
        {
            lastIrreversibleBlockStateSetFoundEventData = d;
            return Task.CompletedTask;
        });
        
        var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 2, "BlockHash", chainId,2,"TransactionId",TransactionStatus.Mined,2);
        await AddBlocksAsync(chainId, blocks);
        
        await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blocks.Last().BlockHash);

        await _blockProcessingService.ProcessAsync(chainId, blocks.Last().BlockHash);

        var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
        bestChain.Block.BlockHash.ShouldBe(blocks.Last().BlockHash);
        bestChain.Block.BlockHeight.ShouldBe(blocks.Last().BlockHeight);

        lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();

        foreach (var block in blocks)
        {
            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.BlockHash);
            blockStateSet.Processed.ShouldBeTrue();
        }
        
        var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
        blockEntities.Count.ShouldBe(2);
        var i = 100;
        foreach (var blockEntity in blockEntities)
        {
            blockEntity.BlockHash.ShouldBe("BlockHash" + i);
            i++;
        }
        
        var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
        transactionEntities.Count.ShouldBe(4);
        var j= 0;
        foreach (var transactionEntity in transactionEntities)
        {
            transactionEntity.Id.ShouldBe(transactionEntity.Metadata.Block.BlockHash + "TransactionId" + j);
            if (j == 1)
            {
                j = 0;
            }
            else
            {
                j++;
            }
        }

        var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
        transferEntities.Count.ShouldBe(8);
        
        var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
        accountBalanceEntities.Count.ShouldBe(2);
        accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount.ShouldBe(-808);
        accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount.ShouldBe(808);
    }
    
    [Fact]
    public async Task Process_NotExistBlockStateSet_Test()
    {
        var chainId = "AELF";
        LastIrreversibleBlockStateSetFoundEventData lastIrreversibleBlockStateSetFoundEventData = null;

        _localEventBus.Subscribe<LastIrreversibleBlockStateSetFoundEventData>(d =>
        {
            lastIrreversibleBlockStateSetFoundEventData = d;
            return Task.CompletedTask;
        });
        
        var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 2, "BlockHash", chainId,2,"TransactionId",TransactionStatus.Mined,2);
        await _blockProcessingService.ProcessAsync(chainId, blocks.Last().BlockHash);

        var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
        bestChain.ShouldBeNull();

        lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
        
        var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
        blockEntities.Count.ShouldBe(0);
        
        var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
        transactionEntities.Count.ShouldBe(0);

        var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
        transferEntities.Count.ShouldBe(0);
        
        var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
        accountBalanceEntities.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task Process_NotInLibBranch_Test()
    {
        var chainId = "AELF";
        LastIrreversibleBlockStateSetFoundEventData lastIrreversibleBlockStateSetFoundEventData = null;

        _localEventBus.Subscribe<LastIrreversibleBlockStateSetFoundEventData>(d =>
        {
            lastIrreversibleBlockStateSetFoundEventData = d;
            return Task.CompletedTask;
        });
        
        var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 10, "BlockHash", chainId,2,"TransactionId",TransactionStatus.Mined,2,confirmed:true);
        await AddBlocksAsync(chainId, blocks, true);
        await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blocks.Last().BlockHash);
        await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(chainId, blocks.Last().BlockHash);
        
        var forkBlocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(105, 10, "BlockHashA", chainId,2,"TransactionId",TransactionStatus.Mined,2,perHash:"BlockHash104");
        await AddBlocksAsync(chainId, forkBlocks);

        await _blockProcessingService.ProcessAsync(chainId, forkBlocks.Last().BlockHash);

        var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
        bestChain.ShouldBeNull();

        lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();

        var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
        blockEntities.Count.ShouldBe(0);
        
        var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
        transactionEntities.Count.ShouldBe(0);

        var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
        transferEntities.Count.ShouldBe(0);
        
        var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
        accountBalanceEntities.Count.ShouldBe(0);
    }
    
    private async Task AddBlocksAsync(string chainId, List<BlockWithTransactionDto> blocks, bool process = false)
    {
        foreach (var block in blocks)
        {
            await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, new BlockStateSet
            {
                Block = block,
                Changes = new(),
                Processed = process
            });
        }
    }
}