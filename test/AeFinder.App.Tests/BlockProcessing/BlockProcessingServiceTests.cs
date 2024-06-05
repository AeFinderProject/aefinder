using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.App.MockApp;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Sdk;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AeFinder.App.BlockProcessing;

public class BlockProcessingServiceTests: AeFinderAppTestBase
{
    private readonly IBlockProcessingService _blockProcessingService;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IReadOnlyRepository<BlockEntity> _blockEntityRepository;
    private readonly IReadOnlyRepository<TransactionEntity> _transactionEntityRepository;
    private readonly IReadOnlyRepository<TransferEntity> _transferEntityRepository;
    private readonly IReadOnlyRepository<AccountBalanceEntity> _accountBalanceEntityRepository;
    private readonly IBlockAttachService _blockAttachService;
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
        _blockAttachService = GetRequiredService<IBlockAttachService>();
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
    
    [Fact]
    public async Task Process_Fork_Test()
    {
        var chainId = "AELF";
        LastIrreversibleBlockStateSetFoundEventData lastIrreversibleBlockStateSetFoundEventData = null;

        _localEventBus.Subscribe<LastIrreversibleBlockStateSetFoundEventData>(d =>
        {
            lastIrreversibleBlockStateSetFoundEventData = d;
            return Task.CompletedTask;
        });
        
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 5, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1);
            await AddBlocksAsync(chainId, blocks);

            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blocks.Last().BlockHash);
            await _blockProcessingService.ProcessAsync(chainId, blocks.Last().BlockHash);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe(blocks.Last().BlockHash);

            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();

            foreach (var block in blocks)
            {
                var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.BlockHash);
                blockStateSet.Processed.ShouldBeTrue();
            }

            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(5);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(5);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(5);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-510);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(510);
        }
        
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(103, 3, "BlockHashA",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 2,perHash:"BlockHash102");
            await AddBlocksAsync(chainId, blocks);

            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blocks.Last().BlockHash);
            await _blockProcessingService.ProcessAsync(chainId, blocks.Last().BlockHash);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe(blocks.Last().BlockHash);

            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();

            foreach (var block in blocks)
            {
                var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, block.BlockHash);
                blockStateSet.Processed.ShouldBeTrue();
            }
            
            var set = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHash103");
            set.Processed.ShouldBeFalse();
            set = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHash104");
            set.Processed.ShouldBeFalse();

            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(6);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(6);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(9);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-930);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(930);
        }
        
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 4, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1,confirmed:true);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe("BlockHashA105");

            lastIrreversibleBlockStateSetFoundEventData.BlockHash.ShouldBe("BlockHash102");

            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(6);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(6);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(9);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-930);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(930);
            
            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHash100");
            blockStateSet.ShouldBeNull();
            blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHash101");
            blockStateSet.ShouldBeNull();
        }

        {
            lastIrreversibleBlockStateSetFoundEventData = null;
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(105, 2, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1,perHash:"BlockHash104");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe("BlockHash106");

            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();

            for (int i = 102; i < 107; i++)
            {
                var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHash"+i);
                blockStateSet.Processed.ShouldBeTrue();
            }
            for (int i = 103; i < 106; i++)
            {
                var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHashA"+i);
                blockStateSet.Processed.ShouldBeFalse();
            }
            
            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(7);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(7);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(7);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-721);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(721);
        }
        
        {
            lastIrreversibleBlockStateSetFoundEventData = null;
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(104, 3, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1,perHash:"BlockHash103", confirmed: true);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe("BlockHash106");

            lastIrreversibleBlockStateSetFoundEventData.BlockHash.ShouldBe("BlockHash106");
            
            var lib = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lib.Block.BlockHash.ShouldBe("BlockHash106");
            
            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(7);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(7);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(7);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-721);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(721);
        }
        
        {
            lastIrreversibleBlockStateSetFoundEventData = null;
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(107, 2, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1,perHash:"BlockHash106",confirmed:true);
            await AddBlocksAsync(chainId, blocks);

            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blocks.Last().BlockHash);
            await _blockProcessingService.ProcessAsync(chainId, blocks.Last().BlockHash);

            var bestChain = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
            bestChain.Block.BlockHash.ShouldBe("BlockHash108");

            lastIrreversibleBlockStateSetFoundEventData.BlockHash.ShouldBe("BlockHash108");
            
            var lib = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lib.Block.BlockHash.ShouldBe("BlockHash108");
            
            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(9);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(9);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(9);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-936);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(936);
        }
    }
    
    [Fact]
    public async Task Process_Fork_Delete_Test()
    {
        var chainId = "AELF";
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 5, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);
            
            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(5);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(5);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(5);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.Count.ShouldBe(2);
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-510);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(510);
        }
        
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(103, 7, "BlockHashA",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 2,perHash:"BlockHash102");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(10);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(10);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(17);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.Count.ShouldBe(1);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(1794);
        }
        
        {
            var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(105, 6, "BlockHash",
                chainId, 1, "TransactionId", TransactionStatus.Mined, 1, perHash:"BlockHash104");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);
            
            var blockEntities = (await _blockEntityRepository.GetQueryableAsync()).ToList();
            blockEntities.Count.ShouldBe(11);

            var transactionEntities = (await _transactionEntityRepository.GetQueryableAsync()).ToList();
            transactionEntities.Count.ShouldBe(11);

            var transferEntities = (await _transferEntityRepository.GetQueryableAsync()).ToList();
            transferEntities.Count.ShouldBe(11);

            var accountBalanceEntities = (await _accountBalanceEntityRepository.GetQueryableAsync()).ToList();
            accountBalanceEntities.Count.ShouldBe(2);
            accountBalanceEntities.First(a => a.Account == "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e").Amount
                .ShouldBe(-1155);
            accountBalanceEntities.First(a => a.Account == "xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4").Amount
                .ShouldBe(1155);
        }
    }
    
    private async Task AddBlocksAsync(string chainId, List<AppSubscribedBlockDto> blocks, bool process = false)
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