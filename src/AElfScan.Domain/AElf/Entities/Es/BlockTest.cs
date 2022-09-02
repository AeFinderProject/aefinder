using System;
using AElf.Indexing.Elasticsearch;
using AElfScan.Entities;
using Nest;

namespace AElfScan.AElf.Entities.Es;

public class BlockTest:AElfScanEntity<Guid>,IIndexBuild
{
    public BlockTest()
    {
        
    }

    public BlockTest(Guid id)
    {
        
    }
    
    [Keyword] public override Guid Id { get; set; }
    public long BlockNumber { get; set; }
    public DateTime BlockTime { get; set; }
    public bool IsConfirmed{get;set;}
}