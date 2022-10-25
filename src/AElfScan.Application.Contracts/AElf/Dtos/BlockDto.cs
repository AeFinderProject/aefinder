using System;
using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class BlockDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
    public bool IsConfirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}
    public List<TransactionDto> Transactions {get;set;}
}