using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Entities;
using Nest;

namespace AElfIndexer.Entities.Es;

public class TransactionIndex:AElfIndexerEntity<string>,IIndexBuild,IBlockchainData
{
    [Keyword]
    public override string Id
    {
        get
        {
            return BlockHash + "_" + TransactionId;
        }
    }
    [Keyword]public string TransactionId { get; set; }
    
    [Keyword]public string ChainId { get; set; }
    
    [Keyword]public string From { get; set; }
    
    [Keyword]public string To { get; set; }
    
    [Keyword]public string BlockHash { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    
    public long BlockHeight { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    [Keyword]public string MethodName { get; set; }
    
    [Text(Index = false)]
    public string Params { get; set; }
    
    [Keyword]public string Signature { get; set; }
    
    /// <summary>
    /// The ranking position of transactions within a block
    /// </summary>
    public int Index{get;set;}
    
    public TransactionStatus Status { get; set; }
    
    public bool Confirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    [Nested(Name = "LogEvents",Enabled = true,IncludeInParent = true,IncludeInRoot = true)]
    public List<LogEvent> LogEvents{get;set;}
}