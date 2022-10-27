using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfScan.Entities;
using Nest;

namespace AElfScan.AElf.Entities.Es;

[ElasticsearchType]
public class Block:AElfScanEntity<string>,IIndexBuild,IBlockchainData
{
    [Keyword]public override string Id { get; set; }
    [Keyword]public string ChainId { get; set; }
    [Keyword]public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    [Keyword]public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword]public string SignerPubkey { get; set; }
    [Keyword]public string Signature { get; set; }
    public bool IsConfirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}
    [Nested(Name = "Transactions",Enabled = true,IncludeInParent = true,IncludeInRoot = true)]
    public IEnumerable<Transaction> Transactions {get;set;}
}