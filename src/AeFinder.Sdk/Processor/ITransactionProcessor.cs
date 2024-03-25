namespace AeFinder.Sdk.Processor;

public interface ITransactionProcessor : IBlockDataProcessor
{
    Task ProcessAsync(Transaction transaction, TransactionContext context);
}