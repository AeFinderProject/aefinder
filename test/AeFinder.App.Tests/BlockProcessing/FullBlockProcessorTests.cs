using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AeFinder.App.BlockState;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.BlockStates;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AeFinder.App.BlockProcessing;

public class FullBlockProcessorTests: AeFinderAppTestBase
{
    private readonly IFullBlockProcessor _fullBlockProcessor;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IObjectMapper _objectMapper;
    
    public FullBlockProcessorTests()
    {
        _fullBlockProcessor = GetRequiredService<IFullBlockProcessor>();
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _objectMapper = GetRequiredService<IObjectMapper>();
    }

    [Fact]
    public async Task Process_Cancel_Test()
    {
        var chainId = "AELF";
        var blocks = BlockCreationHelper.CreateBlockWithTransactionAndTransferredLogEvents(100, 1, "BlockHash", chainId,2,"TransactionId",TransactionStatus.Mined,2);
        
        foreach (var block in blocks)
        {
            await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, new BlockStateSet
            {
                Block = _objectMapper.Map<AppSubscribedBlockDto, BlockWithTransactionDto>(block),
                Changes = new(),
                Processed = false
            });
        }

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAsync<AppProcessingException>(async () =>
            await _fullBlockProcessor.ProcessAsync( _objectMapper.Map<AppSubscribedBlockDto, BlockWithTransactionDto>(blocks.Last()), cts.Token));
    }
}