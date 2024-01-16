using System.Collections.Generic;

namespace AeFinder.BlockChainEventHandler.Core.DTOs;

public class TransactionEto
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
    public int Index{get;set;}
    
    public int Status { get; set; }
    
    public Dictionary<string, string> ExtraProperties {get;set;}

    public List<LogEventEto> LogEvents { get; set; } = new List<LogEventEto>();
}