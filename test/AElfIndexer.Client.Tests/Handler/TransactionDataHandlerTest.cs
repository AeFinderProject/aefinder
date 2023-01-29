using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Handler;

public class TransactionDataHandlerTest : AElfIndexerClientTransactionDataHandlerTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> _repository;
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _blockRepository;
    private readonly IClusterClient _clusterClient;

    public TransactionDataHandlerTest()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo>>();
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
            _clusterClient.GetGrain<IBlockStateSetsGrain<TransactionInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSet();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
    }
}