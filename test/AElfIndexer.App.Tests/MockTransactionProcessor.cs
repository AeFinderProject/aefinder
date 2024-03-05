using System;
using System.Threading.Tasks;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App;

public class MockTransactionProcessor : TransactionProcessorBase, ITransientDependency
{
    public override string GetToAddress(string chainId)
    {
        return null;
    }

    public override string GetMethodName(string chainId)
    {
        return null;
    }

    public override async Task ProcessAsync(Transaction transaction, TransactionContext context)
    {
        if (context.Block.BlockHeight == 100000)
        {
            throw new Exception();
        }
        
        var indexId = transaction.TransactionId;
        var index = new TestTransactionEntity
        {
            Id = indexId,
            MethodName = transaction.MethodName,
            From = transaction.From,
            To = transaction.To
        };
        
        await SaveEntityAsync(index);
    }
}