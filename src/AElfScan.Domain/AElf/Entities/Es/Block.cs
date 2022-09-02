using System;
using System.Collections.Generic;

namespace AElfScan.AElf.Entities.Es;

public class Block:IBlockchainData
{
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