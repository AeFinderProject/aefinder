using System;
using System.Collections.Generic;
using Orleans;

namespace AeFinder.Block.Dtos;

[GenerateSerializer]
public class LogEventDto
{
    [Id(0)]public string ChainId { get; set; }
    [Id(1)]public string BlockHash { get; set; }
    
    /// <summary>
    /// block height
    /// </summary>
    [Id(2)]public long BlockHeight { get; set; }
    [Id(3)]public string PreviousBlockHash { get; set; }

    [Id(4)]public string TransactionId { get; set; }
    
    [Id(5)]public DateTime BlockTime { get; set; }
    
    [Id(6)]public string ContractAddress { get; set; }
    
    [Id(7)]public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    [Id(8)]public int Index { get; set; }
    
    [Id(9)]public bool Confirmed{get;set;}
    
    [Id(10)]public Dictionary<string,string> ExtraProperties {get;set;}
}