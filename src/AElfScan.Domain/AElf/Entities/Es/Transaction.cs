using System;
using System.Collections.Generic;
using Nest;

namespace AElfScan.AElf.Entities.Es;

public class Transaction:IBlockchainData
{
    [Keyword]public string TransactionId { get; set; }
    
    [Keyword]public string ChainId { get; set; }
    
    [Keyword]public string From { get; set; }
    
    [Keyword]public string To { get; set; }
    
    [Keyword]public string BlockHash { get; set; }
    
    public long BlockNumber { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    [Keyword]public string MethodName { get; set; }
    
    [Keyword]public string Params { get; set; }
    
    [Keyword]public string Signature { get; set; }
    
    /// <summary>
    /// 交易在区块内的排序位置
    /// </summary>
    public int Index{get;set;}
    
    public TransactionStatus Status { get; set; }
    
    public bool IsConfirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    public List<LogEvent> LogEvents{get;set;}
}