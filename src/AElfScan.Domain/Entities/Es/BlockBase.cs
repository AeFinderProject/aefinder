using System;
using System.Collections.Generic;
using AElfScan.Entities;
using Nest;

namespace AElfScan.Entities.Es;

public class BlockBase:AElfScanEntity<string>,IBlockchainData
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
    public List<Transaction> Transactions {get;set;}
}