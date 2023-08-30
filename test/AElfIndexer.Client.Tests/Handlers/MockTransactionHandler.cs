using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public class MockTransactionHandler : TransactionDataHandler
{
    private readonly IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> _repository;

    public MockTransactionHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> repository,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<TransactionInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors, ILogger<TransactionDataHandler> logger)
        : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider, dAppDataProvider, blockStateSetProvider,
            dAppDataIndexManagerProvider, processors, logger)
    {
        _repository = repository;
    }

    protected override async Task ProcessTransactionAsync(TransactionInfo transaction)
    {
        var indexId = transaction.TransactionId;
        var index = new TestTransactionIndex
        {
            Id = indexId,
            ChainId = transaction.ChainId,
            MethodName = transaction.MethodName,
            BlockTime = transaction.BlockTime,
            BlockHash = transaction.BlockHash,
            BlockHeight = transaction.BlockHeight,
            PreviousBlockHash = transaction.PreviousBlockHash,
            From = transaction.From,
            To = transaction.To,
            LogEventInfos = transaction.LogEvents,
            Confirmed = transaction.Confirmed
        };
        await _repository.AddOrUpdateAsync(index);
    }
}