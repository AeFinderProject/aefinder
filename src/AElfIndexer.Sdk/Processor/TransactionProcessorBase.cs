namespace AElfIndexer.Sdk;

public abstract class TransactionProcessorBase : BlockDataProcessorBase, ITransactionProcessor
{
    public abstract string GetToAddress(string chainId);
    public abstract string GetMethodName(string chainId);
    public abstract Task ProcessAsync(Transaction transaction, TransactionContext context);
}