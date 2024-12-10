namespace AeFinder;

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