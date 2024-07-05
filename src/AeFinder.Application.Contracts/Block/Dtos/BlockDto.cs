using System;
using System.Collections.Generic;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Block.Dtos;

[GenerateSerializer]
public class BlockDto
{
    [Id(0)]public string Id { get; set; }
    [Id(1)]public string ChainId { get; set; }
    [Id(2)]public string BlockHash { get; set; }
    [Id(3)]public long BlockHeight { get; set; }
    [Id(4)]public string PreviousBlockHash { get; set; }
    [Id(5)]public DateTime BlockTime { get; set; }
    [Id(6)]public string SignerPubkey { get; set; }
    [Id(7)]public string Miner { get; set; }
    [Id(8)]public string Signature { get; set; }
    [Id(9)]public bool Confirmed{get;set;}
    [Id(10)]public Dictionary<string,string> ExtraProperties {get;set;}

    // public List<TransactionDto> Transactions {get;set;}
    [Id(11)]public List<string> TransactionIds { get; set; } = new();
    [Id(12)]public int LogEventCount { get; set; }
}