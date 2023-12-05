using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Handlers;

public class LogEventDataHandlerTests : AElfIndexerClientLogEventHandlerTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestTransferredIndex, LogEventInfo> _repository;
    private readonly IClusterClient _clusterClient;

    public LogEventDataHandlerTests()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestTransferredIndex, LogEventInfo>>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }


    [Fact]
    public async Task LogEvent_Block_With_LogEvent_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(
            100, 10, "BlockHash", chainId, 1, "TransactionId",
            TransactionStatus.Mined, 1);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        var transferredIndex = await _repository.GetListAsync();
        transferredIndex.Item2.Count.ShouldBe(10);
        transferredIndex.Item2.First().Amount.ShouldBe(100);
        transferredIndex.Item2.First().Id.ShouldBe("100TransactionId00100");
        transferredIndex.Item2.Last().Amount.ShouldBe(109);
        transferredIndex.Item2.Last().Id.ShouldBe("109TransactionId00109");
        transferredIndex.Item2.All(t => t.Symbol.Equals("TEST")).ShouldBeTrue();
        transferredIndex.Item2.All(t => t.FromAccount.IsNullOrEmpty()).ShouldBeFalse();
        transferredIndex.Item2.All(t => t.ToAccount.IsNullOrEmpty()).ShouldBeFalse();

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<LogEventInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
    }


    [Fact]
    public async Task LogEvent_Block_Without_LogEvent_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(
            100, 1, "BlockHash", chainId, 2, "TransactionId", TransactionStatus.Mined, 1);
        var blocksWithoutTransaction = MockHandlerHelper.CreateBlock(
            101, 8, "BlockHash", chainId, "BlockHash100");
        var lastBlock = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(
            109, 1, "BlockHash", chainId, 2, "TransactionId", TransactionStatus.Mined, 1);
        blocks.AddRange(blocksWithoutTransaction);
        blocks.AddRange(lastBlock);

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        var transferredIndex = await _repository.GetListAsync();
        transferredIndex.Item2.Count.ShouldBe(4);
        transferredIndex.Item2.First().Amount.ShouldBe(100);
        transferredIndex.Item2.First().Id.ShouldBe("100TransactionId00100");
        transferredIndex.Item2.Last().Amount.ShouldBe(109);
        transferredIndex.Item2.Last().Id.ShouldBe("109TransactionId10109");
        transferredIndex.Item2.All(t => t.Symbol.Equals("TEST")).ShouldBeTrue();
        transferredIndex.Item2.All(t => t.FromAccount.IsNullOrEmpty()).ShouldBeFalse();
        transferredIndex.Item2.All(t => t.ToAccount.IsNullOrEmpty()).ShouldBeFalse();

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<LogEventInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
    }

    [Fact]
    public async Task LogEvent_Block_With_Two_LogEvent_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = MockHandlerHelper.CreateBlock(
            100, 3, "BlockHash", chainId, "");
        var blockWithTwoLogEvent = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(
            103, 1, "BlockHash", chainId, 1, "TransactionId", TransactionStatus.Mined, 2);
        var blocksWithoutTransaction = MockHandlerHelper.CreateBlock(
            104, 6, "BlockHash", chainId, "BlockHash103");
        blocks.AddRange(blockWithTwoLogEvent);
        blocks.AddRange(blocksWithoutTransaction);

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        var transferredIndex = await _repository.GetListAsync();
        transferredIndex.Item2.Count.ShouldBe(2);
        transferredIndex.Item2.First().Amount.ShouldBe(103);
        transferredIndex.Item2.First().Id.ShouldBe("103TransactionId00103");
        transferredIndex.Item2.Last().Amount.ShouldBe(104);
        transferredIndex.Item2.Last().Id.ShouldBe("103TransactionId00104");
        transferredIndex.Item2.All(t => t.Symbol.Equals("TEST")).ShouldBeTrue();
        transferredIndex.Item2.All(t => t.FromAccount.IsNullOrEmpty()).ShouldBeFalse();
        transferredIndex.Item2.All(t => t.ToAccount.IsNullOrEmpty()).ShouldBeFalse();

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<LogEventInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
    }
}