namespace AElfIndexer.Sdk.Processor;

public interface ITransactionProcessor
{
    Task ProcessAsync(Transaction transaction, TransactionContext context);
}