using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Block.Dtos;

public class GetBlocksInput
{
    public string ChainId { get; set; }
    [Range(1, long.MaxValue)]
    public long StartBlockHeight { get; set; }
    [Range(1, long.MaxValue)]
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public bool HasTransaction { get; set; } = false;
    public List<FilterContractEventInput> Events { get; set; }
    public string BlockHash { get; set; }
}