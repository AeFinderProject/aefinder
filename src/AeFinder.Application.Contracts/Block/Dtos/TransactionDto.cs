using System;
using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class TransactionDto
{
    public string TransactionId { get; set; }
    
    public string ChainId { get; set; }
    
    public string From { get; set; }
    
    public string To { get; set; }
    
    public string BlockHash { get; set; }
    
    public long BlockHeight { get; set; }

    public string PreviousBlockHash { get; set; }

    public DateTime BlockTime { get; set; }
    
    public string MethodName { get; set; }
    
    public string Params { get; set; }
    
    public string Signature { get; set; }
    
    /// <summary>
    /// The ranking position of transactions within a block
    /// </summary>
    public int Index{get;set;}
    
    public TransactionStatus Status { get; set; }
    
    public bool Confirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    public List<LogEventDto> LogEvents{get;set;}
}