namespace AElfIndexer.Grains.State.Client;

public class LogEventInfo : BlockChainDataBase
{
    public string TransactionId { get; set; }
    
    public string ContractAddress { get; set; }
    
    public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
}