using System;
using System.Collections.Generic;
using AElf;
using AElf.EntityMapping.Sharding;
using Nest;

namespace AElfIndexer.Entities.Es;

public class BlockBase:AElfIndexerEntity<string>,IBlockchainData
{
    [Keyword]public override string Id { get; set; }
    
    [ShardPropertyAttributes("ChainId",1)]
    [Keyword]
    public string ChainId { get; set; } 
    
    [CollectionRouteKey]
    [Keyword]
    public string BlockHash { get; set; }
    
    [ShardPropertyAttributes("BlockHeight",2)]
    public long BlockHeight { get; set; }
    
    [Keyword]public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword]public string SignerPubkey { get; set; }
    [CollectionRouteKey]
    [Keyword]
    public string Miner
    {
        get
        {
            return AElf.Types.Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(SignerPubkey)).ToBase58();
        }
    }

    [Keyword]public string Signature { get; set; }
    public bool Confirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}

}