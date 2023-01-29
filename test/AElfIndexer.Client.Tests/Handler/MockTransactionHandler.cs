using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfIndexer.Handler;

public class MockTransactionHandler : TransactionDataHandler
{
    private readonly IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> _repository;

    public MockTransactionHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TestTransactionIndex, TransactionInfo> repository,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors, ILogger<TransactionDataHandler> logger)
        : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider, processors, logger)
    {
        _repository = repository;
    }

    protected override async Task ProcessTransactionsAsync(List<TransactionInfo> transactions)
    {
        if (!transactions.Any())
        {
            return;
        }

        foreach (var transaction in transactions)
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
}