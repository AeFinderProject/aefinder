namespace AElfIndexer.Sdk;

public abstract class TransactionProcessorBase : BlockDataProcessorBase, ITransactionProcessor
{
    public abstract Task ProcessAsync(Transaction transaction, TransactionContext context);
}