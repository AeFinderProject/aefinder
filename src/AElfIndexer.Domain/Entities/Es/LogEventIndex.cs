using System;
using System.Collections.Generic;
using AElf.EntityMapping.Entities;
using AElf.EntityMapping.Sharding;
using AElfIndexer.Entities;
using Nest;

namespace AElfIndexer.Entities.Es;

public class LogEventIndex:AElfIndexerEntity<string>,IEntityMappingEntity,IBlockchainData
{
    [Keyword]
    public override string Id
    {
        get
        {
            return BlockHash + "_" + TransactionId + "_" + Index;
        }
    }
    [ShardPropertyAttributes("ChainId",1)]
    [Keyword]
    public string ChainId { get; set; }
    
    [CollectionRouteKey]
    [Keyword]
    public string BlockHash { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    /// <summary>
    /// block height
    /// </summary>
    [ShardPropertyAttributes("BlockHeight",2)]
    public long BlockHeight { get; set; }
    [CollectionRouteKey]
    [Keyword]public string TransactionId { get; set; }
    
    public DateTime BlockTime { get; set; }
    [CollectionRouteKey]
    [Keyword]public string ContractAddress { get; set; }
    
    [Keyword]public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
    
    public bool Confirmed{get;set;}
    
    public Dictionary<string,string> ExtraProperties {get;set;}
}