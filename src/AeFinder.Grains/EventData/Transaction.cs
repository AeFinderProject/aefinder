namespace AeFinder.Grains.EventData;

[GenerateSerializer]
public class Transaction
{
    [Id(0)] public string TransactionId { get; set; }
    
    [Id(1)] public string ChainId { get; set; }
    
    [Id(2)] public string From { get; set; }
    
    [Id(3)] public string To { get; set; }
    
    [Id(4)] public string BlockHash { get; set; }
    
    [Id(5)] public string PreviousBlockHash { get; set; }
    [Id(6)] public long BlockHeight { get; set; }
    
    [Id(7)] public DateTime BlockTime { get; set; }
    
    [Id(8)] public string MethodName { get; set; }

    [Id(9)] public string Params { get; set; }
    
    [Id(10)] public string Signature { get; set; }
    
    /// <summary>
    /// The ranking position of transactions within a block
    /// </summary>
    [Id(11)] public int Index{get;set;}
    
    [Id(12)] public TransactionStatus Status { get; set; }
    
    [Id(13)] public bool Confirmed{get;set;}
    
    [Id(14)] public Dictionary<string,string> ExtraProperties {get;set;}
    
    [Id(15)] public List<LogEvent> LogEvents{get;set;}
}