using AeFinder.App.BlockProcessing;
using AeFinder.App.BlockState;
using AeFinder.Block.Dtos;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Sdk.Processor;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using Orleans.TestingHost;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;
using Volo.Abp.Threading;
using Transaction = AeFinder.Sdk.Processor.Transaction;

namespace AeFinder.App.TestBase;

public abstract class AeFinderAppTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule> 
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IBlockProcessingContext _blockProcessingContext;
    
    protected string ChainId = "AELF";
    protected string BlockHash = "6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b";
    protected string PreviousBlockHash = "d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35";
    protected long BlockHeight = 100;

    protected AeFinderAppTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _blockProcessingContext = GetRequiredService<IBlockProcessingContext>();

        AsyncHelper.RunSync(InitializeAsync);
    }

    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }
    
    protected async Task InitializeAsync()
    {
        await _appBlockStateSetProvider.AddBlockStateSetAsync(ChainId, new BlockStateSet
        {
            Block = new BlockWithTransactionDto
            {
                ChainId = ChainId,
                BlockHash = BlockHash,
                PreviousBlockHash = PreviousBlockHash,
                BlockHeight = BlockHeight
            },
            Changes = new (),
            Processed = false
        });

        _blockProcessingContext.SetContext(ChainId, BlockHash, BlockHeight,
            DateTime.UtcNow);
    }

    protected LogEventContext GenerateLogEventContext<T>(T eventData, Transaction transaction = null)
        where T : IEvent<T>
    {
        var logEvent = eventData.ToLogEvent().ToSdkLogEvent();

        var context = new LogEventContext
        {
            ChainId = ChainId,
            Block = new LightBlock
            {
                BlockHash = BlockHash,
                BlockHeight = BlockHeight,
                BlockTime = DateTime.UtcNow,
                PreviousBlockHash = PreviousBlockHash
            },
            Transaction = new Transaction()
            {
                TransactionId = "4e07408562bedb8b60ce05c1decfe3ad16b72230967de01f640b7e4729b49fce",
                From = "2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz",
                To = "2ktxGpyiYCjFU5KwuXtbBckczX6uPmEtesJEsQPqMukcHZFY9a",
                Index = 1,
                MethodName = "TestMethod",
                Status = Sdk.Processor.TransactionStatus.Mined
            },
            LogEvent = logEvent
        };

        if (transaction != null)
        {
            context.Transaction = transaction;
        }

        return context;
    }
    
    protected TransactionContext GenerateTransactionContext(Transaction transaction)
    {
        return new TransactionContext()
        {
            ChainId = ChainId,
            Block = new LightBlock
            {
                BlockHash = BlockHash,
                BlockHeight = BlockHeight,
                BlockTime = DateTime.UtcNow,
                PreviousBlockHash = PreviousBlockHash
            }
        };
    }
    
    protected BlockContext GenerateBlockContext(Transaction transaction)
    {
        return new BlockContext()
        {
            ChainId = ChainId
        };
    }
}