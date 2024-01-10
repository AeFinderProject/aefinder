using System.Collections.Generic;

namespace AElfIndexer.Block.Dtos;

public class FilterTransactionInput
{
    public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}