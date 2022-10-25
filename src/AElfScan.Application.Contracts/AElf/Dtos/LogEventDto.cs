using System;
using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class LogEventDto
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    
    /// <summary>
    /// block height
    /// </summary>
    public long BlockNumber { get; set; }
    
    public string TransactionId { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    public string ContractAddress { get; set; }
    
    public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
    
    public bool IsConfirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
}