using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AElfIndexer.Block.Dtos;

public class BlockDto:EntityDto<string>
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
    public bool IsConfirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    // TODO: Set these properties.
    public List<string> TransactionIds { get; set; } = new();
    public int LogEventCount { get; set; }
}