using System.Collections.Generic;
using AeFinder.Block.Dtos;
using Orleans;

namespace AeFinder.BlockScan;

[GenerateSerializer]
public class SubscribedBlockDto
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string AppId { get; set; }
    [Id(2)] public string Version { get; set; }
    [Id(3)] public List<AppSubscribedBlockDto> Blocks { get; set; }
    [Id(4)] public string PushToken { get; set; }
}