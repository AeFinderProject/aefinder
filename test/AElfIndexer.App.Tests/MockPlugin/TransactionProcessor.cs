using System;
using System.Threading.Tasks;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.MockPlugin;

public class TransactionProcessor : TransactionProcessorBase, ITransientDependency
{
    public override async Task ProcessAsync(Transaction transaction, TransactionContext context)
    {
        if (context.Block.BlockHeight == 100000)
        {
            throw new Exception();
        }
        
        var indexId = transaction.TransactionId;
        var index = new TransactionEntity
        {
            Id = indexId,
            MethodName = transaction.MethodName,
            From = transaction.From,
            To = transaction.To
        };
        
        await SaveEntityAsync(index);
    }
}