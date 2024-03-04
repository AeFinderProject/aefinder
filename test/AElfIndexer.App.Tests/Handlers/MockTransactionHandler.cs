using System;
using System.Threading.Tasks;
using AElfIndexer.Sdk;

namespace AElfIndexer.App.Handlers;

public class MockTransactionHandler : TransactionProcessorBase
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
        var index = new TestTransactionIndex(indexId)
        {
            MethodName = transaction.MethodName,
            From = transaction.From,
            To = transaction.To
        };
        
        await SaveEntityAsync(index);
    }
}