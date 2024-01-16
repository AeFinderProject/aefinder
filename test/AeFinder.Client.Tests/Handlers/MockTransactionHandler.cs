using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Client.Providers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Client.Handlers;

public class MockTransactionHandler : TransactionDataHandler
{
    private readonly IAeFinderClientEntityRepository<TestTransactionIndex, TransactionInfo> _repository;

    public MockTransactionHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAeFinderClientEntityRepository<TestTransactionIndex, TransactionInfo> repository,
        IAeFinderClientInfoProvider aefinderClientInfoProvider,
        IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<TransactionInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors, ILogger<TransactionDataHandler> logger)
        : base(clusterClient, objectMapper, aefinderClientInfoProvider, dAppDataProvider, blockStateSetProvider,
            dAppDataIndexManagerProvider, processors, logger)
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