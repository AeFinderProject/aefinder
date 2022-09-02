using System;
using System.Collections.Generic;

namespace AElfScan.AElf.Entities.Es;

public class Transaction:IBlockchainData
{
    public string TransactionId { get; set; }
    
    public string ChainId { get; set; }
    
    public string From { get; set; }
    
    public string To { get; set; }
    
    public string BlockHash { get; set; }
    
    public long BlockNumber { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    public string MethodName { get; set; }
    
    public string Params { get; set; }
    
    public string Signature { get; set; }
    
    /// <summary>
    /// 交易在区块内的排序位置
    /// </summary>
    public int Index{get;set;}
    
    public TransactionStatus Status { get; set; }
    
    public bool IsConfirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    public List<LogEvent> LogEvents{get;set;}
}