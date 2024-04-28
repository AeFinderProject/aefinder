using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElf.EntityMapping.Sharding;
using Nest;

namespace AeFinder.Entities.Es;

public class TransactionIndex:AeFinderEntity<string>,IEntityMappingEntity,IBlockchainData
{
    [Keyword]
    public override string Id
    {
        get
        {
            return BlockHash + "_" + TransactionId;
        }
    }
    [CollectionRouteKey]
    [Keyword]
    public string TransactionId { get; set; }
    
    [ShardPropertyAttributes("ChainId",1)]
    [Keyword]
    public string ChainId { get; set; }
    [CollectionRouteKey]
    [Keyword]public string From { get; set; }
    [CollectionRouteKey]
    [Keyword]public string To { get; set; }
    
    [CollectionRouteKey]
    [Keyword]
    public string BlockHash { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    
    [ShardPropertyAttributes("BlockHeight",2)]
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