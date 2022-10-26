using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class ContractInput
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }
}