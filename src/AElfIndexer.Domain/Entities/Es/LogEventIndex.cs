using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Entities;
using Nest;

namespace AElfIndexer.Entities.Es;

public class LogEventIndex:AElfIndexerEntity<string>,IIndexBuild,IBlockchainData
{
    [Keyword]
    public override string Id
    {
        get
        {
            return BlockHash + "_" + TransactionId + "_" + Index;
        }
    }
    [Keyword]public string ChainId { get; set; }
    [Keyword]public string BlockHash { get; set; }
    [Keyword]public string PreviousBlockHash { get; set; }
    /// <summary>
    /// block height
    /// </summary>
    public long BlockHeight { get; set; }
    
    [Keyword]public string TransactionId { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    [Keyword]public string ContractAddress { get; set; }
    
    [Keyword]public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
    
    public bool Confirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
}