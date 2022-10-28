using System.Collections.Generic;

namespace AElfScan.AElf.Dtos;

public class GetLogEventsInput
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public List<EventInput> Events { get; set; }
}