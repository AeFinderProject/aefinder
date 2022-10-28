using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class EventInput
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }
}