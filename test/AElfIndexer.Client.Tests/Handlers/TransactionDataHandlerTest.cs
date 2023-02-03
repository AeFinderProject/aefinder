using System;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Handlers;

public class TransactionDataHandlerTest : AElfIndexerClientTransactionDataHandlerTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> _repository;
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _blockRepository;
    private readonly IAElfIndexerClientEntityRepository<TestTransferredIndex, LogEventInfo> _transferredRepository;
    private readonly IClusterClient _clusterClient;

    public TransactionDataHandlerTest()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo>>();
        _transferredRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TestTransferredIndex, LogEventInfo>>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }


    [Fact]
    public async Task Transaction_Block_With_Transaction_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtos(
            100, 10, "BlockHash", chainId, 2, "TransactionId", TransactionStatus.Mined);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var transactionIndex = await _repository.GetListAsync();
        transactionIndex.Item2.Count.ShouldBe(20);
        transactionIndex.Item2.First().Id.ShouldBe("BlockHash100TransactionId0");
        transactionIndex.Item2.Last().Id.ShouldBe("BlockHash109TransactionId1");

        var transferIndex = await _transferredRepository.GetListAsync();
        ;
    }

    [Fact]
    public async Task Transaction_Block_With_TransactionAndLogEvent_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(
            100, 10, "BlockHash", chainId, 1, "TransactionId",
            TransactionStatus.Mined, 1);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var transactionIndex = await _repository.GetListAsync();
        transactionIndex.Item2.Count.ShouldBe(10);
        transactionIndex.Item2.First().Id.ShouldBe("BlockHash100TransactionId0");
        transactionIndex.Item2.Last().Id.ShouldBe("BlockHash109TransactionId0");

        var transferredIndex = await _transferredRepository.GetListAsync();
        transferredIndex.Item2.Count.ShouldBe(10);
        transferredIndex.Item2.First().Amount.ShouldBe(100);
        transferredIndex.Item2.First().Id.ShouldBe("BlockHash100TransactionId00100");
        transferredIndex.Item2.Last().Amount.ShouldBe(109);
        transferredIndex.Item2.Last().Id.ShouldBe("BlockHash109TransactionId00109");
        transferredIndex.Item2.All(t => t.Symbol.Equals("TEST")).ShouldBeTrue();
        transferredIndex.Item2.All(t => t.FromAccount.IsNullOrWhiteSpace()).ShouldBeFalse();
        transferredIndex.Item2.All(t => t.ToAccount.IsNullOrWhiteSpace()).ShouldBeFalse();
    }

    [Fact]
    public async Task Transaction_Block_Without_Transaction_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtos(
            100, 1, "BlockHash", chainId, 2, "TransactionId", TransactionStatus.Mined);
        var blocksWithoutTransaction = MockHandlerHelper.CreateBlock(
            101, 8, "BlockHash", chainId, "BlockHash100");
        var lastBlock = MockHandlerHelper.CreateBlockWithTransactionDtos(
            109, 1, "BlockHash", chainId, 2, "TransactionId", TransactionStatus.Mined);
        blocks.AddRange(blocksWithoutTransaction);
        blocks.AddRange(lastBlock);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var transactionIndex = await _repository.GetListAsync();
        transactionIndex.Item2.Count.ShouldBe(4);
        transactionIndex.Item2.First().Id.ShouldBe("BlockHash100TransactionId0");
        transactionIndex.Item2.Last().Id.ShouldBe("BlockHash109TransactionId1");

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<TransactionInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
    }
}