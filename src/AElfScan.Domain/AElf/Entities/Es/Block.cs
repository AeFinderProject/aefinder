using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfScan.Entities;
using Nest;

namespace AElfScan.AElf.Entities.Es;

public class Block:AElfScanEntity<Guid>,IIndexBuild,IBlockchainData
{
    public Block()
    {
        
    }

    public Block(Guid id)
    {
        
    }
    
    [Keyword] public override Guid Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
    public bool IsConfirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}
    public List<Transaction> Transactions {get;set;}
}