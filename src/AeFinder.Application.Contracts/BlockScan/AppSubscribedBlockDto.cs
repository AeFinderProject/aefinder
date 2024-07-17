using System;
using System.Collections.Generic;
using Orleans;

namespace AeFinder.BlockScan;

[GenerateSerializer]
public class AppSubscribedBlockDto
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string BlockHash { get; set; }
    [Id(2)] public long BlockHeight { get; set; }
    [Id(3)] public string PreviousBlockHash { get; set; }
    [Id(4)] public DateTime BlockTime { get; set; }
    [Id(5)] public bool Confirmed{get;set;}
    [Id(6)] public List<AppSubscribedTransactionDto> Transactions { get; set; } = new();
}

[GenerateSerializer]
public class AppSubscribedTransactionDto
{
    [Id(0)] public string TransactionId { get; set; }
    [Id(1)] public string From { get; set; }
    [Id(2)] public string To { get; set; }
    [Id(3)] public string MethodName { get; set; }
    [Id(4)] public string Params { get; set; }
    [Id(5)] public int Index{get;set;}
    [Id(6)] public TransactionStatus Status { get; set; }
    [Id(7)] public Dictionary<string,string> ExtraProperties {get;set;} = new();
    [Id(8)] public List<AppSubscribedLogEventDto> LogEvents{get;set;} = new();
}

[GenerateSerializer]
public class AppSubscribedLogEventDto
{
    [Id(0)] public string ContractAddress { get; set; }
    [Id(1)] public string EventName { get; set; }
    [Id(2)] public int Index { get; set; }
    [Id(3)] public Dictionary<string,string> ExtraProperties {get;set;} = new();
}
