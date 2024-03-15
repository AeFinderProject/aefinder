using System;
using System.Collections.Generic;
using AElf.EntityMapping.Elasticsearch.Linq;
using Nest;

namespace AeFinder.Entities.Es;

[NestedAttributes("LogEvents")]
public class LogEvent:IBlockchainData
{
    [Keyword]public string ChainId { get; set; }

    [Keyword]public string BlockHash { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    public long BlockHeight { get; set; }
    
    public DateTime BlockTime { get; set; }
    [Keyword]public string TransactionId { get; set; }

    [Keyword]public string ContractAddress { get; set; }
    
    [Keyword]public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
    
    public bool Confirmed{get;set;}

    public Dictionary<string,string> ExtraProperties {get;set;}
}