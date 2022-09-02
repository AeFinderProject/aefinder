using System;
using System.Collections.Generic;

namespace AElfScan.AElf.Entities.Es;

public class LogEvent:IBlockchainData
{
    public string ChainId { get; set; }
    /// <summary>
    /// 区块Hash
    /// </summary>
    public string BlockHash { get; set; }
    
    /// <summary>
    /// 区块高度
    /// </summary>
    public long BlockNumber { get; set; }
    
    /// <summary>
    /// 交易Id
    /// </summary>
    public string TransactionId { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    public string ContractAddress { get; set; }
    
    public string EventName { get; set; }
    
    /// <summary>
    /// 事件在交易内的排序位置 or Block内的排序位置？
    /// </summary>
    public int Index { get; set; }
    
    public bool IsConfirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
}