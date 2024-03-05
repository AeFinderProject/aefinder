using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App;

public class MockTokenTransferredProcessor : LogEventProcessorBase<Transferred>, ITransientDependency
{
    public override string GetContractAddress(string chainId)
    {
        return "TokenContractAddress";
    }

    public override async Task ProcessAsync(Transferred logEvent, LogEventContext context)
    {
        if (context.Block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var transfer = new TestTransferEntity
        {
            Id = context.Transaction.TransactionId + context.LogEvent.Index + logEvent.Amount,
            FromAccount = logEvent.From.ToBase58(),
            ToAccount = logEvent.To.ToBase58(),
            Symbol = logEvent.Symbol,
            Amount = logEvent.Amount
        };
        await SaveEntityAsync(transfer);
    }
}