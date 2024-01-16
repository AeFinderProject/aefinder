namespace AeFinder.Grains.State.Client;

public class TransactionBase : BlockChainDataBase
{
    public string TransactionId { get; set; }

    public string From { get; set; }

    public string To { get; set; }

    public string MethodName { get; set; }

    public string Params { get; set; }

    public string Signature { get; set; }

    /// <summary>
    /// The ranking position of transactions within a block
    /// </summary>
    public int Index { get; set; }

    public TransactionStatus Status { get; set; }
}