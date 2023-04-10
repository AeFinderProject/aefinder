using System;

namespace AElfIndexer.Entities.Es;

public interface IBlockchainData
{
    string ChainId {get;set;}
    string BlockHash { get; set; }
    string PreviousBlockHash { get; set; }
    long BlockHeight { get; set; }
    DateTime BlockTime{get;set;}
    bool Confirmed{get;set;}
}