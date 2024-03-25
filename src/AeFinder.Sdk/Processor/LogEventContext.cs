namespace AeFinder.Sdk.Processor;

public class ContextBase
{
    public string ChainId { get; set; } 
}

public class LogEventContext : ContextBase
{
    public LightBlock Block { get; set; } 
    public Transaction Transaction { get; set; } 
    public LogEvent LogEvent { get; set; } 
}

public class TransactionContext : ContextBase
{
    public LightBlock Block { get; set; } 
}

public class BlockContext : ContextBase
{
}

public class LightBlock
{
    public string BlockHash { get; set; } 
    public long BlockHeight { get; set; } 
    public string PreviousBlockHash { get; set; } 
    public DateTime BlockTime { get; set; } 
}

public class Block : LightBlock
{
    public string SignerPubkey { get; set; }
    public string Miner { get; set; }
    public string Signature { get; set; }
    public Dictionary<string,string> ExtraProperties {get;set;}
    public List<string> TransactionIds { get; set; } = new();
    public int LogEventCount { get; set; }
}

public class Transaction
{
    public string TransactionId { get; set; } 
    public string From { get; set; } 
    public string To { get; set; } 
    public string MethodName { get; set; } 
    public string Params { get; set; } 
    public int Index { get; set; } 
    public TransactionStatus Status { get; set; } 
    public Dictionary<string, string> ExtraProperties { get; set; } 
}

public class LogEvent
{
    public string ContractAddress { get; set; } 
    public string EventName { get; set; } 
    public Dictionary<string, string> ExtraProperties { get; set; } 
    public int Index { get; set; } 
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