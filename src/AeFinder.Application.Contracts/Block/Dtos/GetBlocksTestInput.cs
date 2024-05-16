using System.Collections.Generic;

namespace AeFinder.Block.Dtos;

public class GetBlocksTestInput
{
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public bool HasTransaction { get; set; } = false;
    public List<FilterContractEventInput> Events { get; set; }
    public string BlockHash { get; set; }
    
    public long SearAfterBlockHeight { get; set; }
    
    public string SearAfterCHainId { get; set; }
    
    public string StartWithStr { get; set; }
    public string EndWithStr { get; set; }
    public string ContainsStr { get; set; }
}