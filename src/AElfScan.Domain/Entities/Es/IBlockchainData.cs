using System;

namespace AElfScan.Entities.Es;

public interface IBlockchainData
{
    string ChainId {get;set;}
    string BlockHash { get; set; }
    string PreviousBlockHash { get; set; }
    long BlockNumber { get; set; }
    DateTime BlockTime{get;set;}
    bool IsConfirmed{get;set;}
}