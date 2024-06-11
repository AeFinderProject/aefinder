using System.Collections.Generic;
using AeFinder.Block.Dtos;

namespace AeFinder.BlockScan;

public class SubscribedBlockDto
{
    public string ChainId { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
    public List<AppSubscribedBlockDto> Blocks { get; set; }
    public string PushToken { get; set; }
}