using System;
using System.Threading.Tasks;
using AeFinder.Sdk.Processor;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.MockApp;

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