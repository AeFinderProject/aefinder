using System;

namespace AElfScan.AElf.Entities.Es;

public interface IBlockchainData
{
    string ChainId {get;set;}
    string BlockHash { get; set; }
    long BlockNumber { get; set; }
    DateTime BlockTime{get;set;}
    bool IsConfirmed{get;set;}
}