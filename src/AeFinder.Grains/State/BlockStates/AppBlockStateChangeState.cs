namespace AeFinder.Grains.State.BlockStates;

[GenerateSerializer]
public class AppBlockStateChangeState
{
    [Id(0)] public long BlockHeight { get; set; }
    [Id(1)] public Dictionary<string, BlockStateChange> Changes { get; set; }
}

[GenerateSerializer]
public class BlockStateChange
{
    [Id(0)] public string Key { get; set; }
    [Id(1)] public string Type { get; set; }
}