using System;
using System.Collections.Generic;

namespace AeFinder.BlockScan;

public class AppSubscribedBlockDto
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public bool Confirmed{get;set;}
    public List<AppSubscribedTransactionDto> Transactions { get; set; } = new();
}

public class AppSubscribedTransactionDto
{
    public string TransactionId { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string MethodName { get; set; }
    public string Params { get; set; }
    public int Index{get;set;}
    public TransactionStatus Status { get; set; }
    public Dictionary<string,string> ExtraProperties {get;set;} = new();
    public List<AppSubscribedLogEventDto> LogEvents{get;set;} = new();
}

public class AppSubscribedLogEventDto
{
    public string ContractAddress { get; set; }
    public string EventName { get; set; }
    public int Index { get; set; }
    public Dictionary<string,string> ExtraProperties {get;set;} = new();
}
