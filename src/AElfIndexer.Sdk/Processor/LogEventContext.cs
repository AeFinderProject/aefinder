namespace AElfIndexer.Sdk.Processor;

public class ContextBase
{
    public string ChainId { get; } 
}

public class BlockContext : ContextBase
{
}

public class LogEventContext : ContextBase
{
    public LightBlock Block { get; }
    public Transaction Transaction { get; }
    public LogEvent LogEvent { get; }
}

public class TransactionContext : ContextBase
{
    public LightBlock Block { get; }
}

public class LightBlock
{
    public string BlockHash { get; }
    public long BlockHeight { get; }
    public string PreviousBlockHash { get; }
    public DateTime BlockTime { get; }
    public bool Confirmed{ get; }
}

public class Block : LightBlock
{
    public string SignerPubkey { get; }
    public Dictionary<string, string> ExtraProperties { get; }
}

public class Transaction
{
    public string TransactionId { get; }
    public string From { get; }
    public string To { get; }
    public string MethodName { get; }
    public string Params { get; }
    public int Index { get; }
    public TransactionStatus Status { get; }
    public Dictionary<string, string> ExtraProperties { get; }
}

public class LogEvent
{
    public string ContractAddress { get; }
    public string EventName { get; }
    public Dictionary<string, string> ExtraProperties { get; }
    public int Index { get; }
}

public enum TransactionStatus
{
    /// <summary>
    /// The execution result of the transaction does not exist.
    /// </summary>
    NotExisted = 0,
    /// <summary>
    /// The transaction is in the transaction pool waiting to be packaged.
    /// </summary>
    Pending = 1,
    /// <summary>
    /// Transaction execution failed.
    /// </summary>
    Failed = 2,
    /// <summary>
    /// The transaction was successfully executed and successfully packaged into a block.
    /// </summary>
    Mined = 3,
    /// <summary>
    /// When executed in parallel, there are conflicts with other transactions.
    /// </summary>
    Conflict = 4,
    /// <summary>
    /// The transaction is waiting for validation.
    /// </summary>
    PendingValidation = 5,
    /// <summary>
    /// Transaction validation failed.
    /// </summary>
    NodeValidationFailed = 6,
}