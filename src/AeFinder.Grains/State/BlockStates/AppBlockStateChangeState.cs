namespace AeFinder.Grains.State.BlockStates;


public class AppBlockStateChangeState
{
    public long BlockHeight { get; set; }
    public Dictionary<string, BlockStateChange> Changes { get; set; }
}

public class BlockStateChange
{
    public string Key { get; set; }
    public string Type { get; set; }
}