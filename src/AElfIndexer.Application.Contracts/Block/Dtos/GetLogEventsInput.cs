using System.Collections.Generic;

namespace AElfIndexer.Block.Dtos;

public class GetLogEventsInput
{
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
    public List<FilterContractEventInput> Events { get; set; }
}