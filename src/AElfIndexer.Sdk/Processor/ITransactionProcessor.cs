namespace AElfIndexer.Sdk;

public interface ITransactionProcessor
{
    string GetToAddress(string chainId);
    string GetMethodName(string chainId);
    Task ProcessAsync(Transaction transaction, TransactionContext context);
}