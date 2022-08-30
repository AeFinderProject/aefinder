using System;

namespace AElfScan.AElf.Dtos;

public class BlockEventDataDto
{
    public long BlockNumber { get; set; }
    // public DateTime BlockTime{get;set;}
    public bool IsConfirmed{get;set;}
}