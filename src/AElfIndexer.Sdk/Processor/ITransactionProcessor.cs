namespace AElfIndexer.Sdk;

public interface ITransactionProcessor : IBlockDataProcessor
{
    Task ProcessAsync(Transaction transaction, TransactionContext context);
}