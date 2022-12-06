using System.Collections.Generic;

namespace AElfIndexer.Block.Dtos;

public class FilterContractEventInput
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }
}