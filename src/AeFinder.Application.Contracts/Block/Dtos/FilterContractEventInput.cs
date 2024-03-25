using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class FilterContractEventInput
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}