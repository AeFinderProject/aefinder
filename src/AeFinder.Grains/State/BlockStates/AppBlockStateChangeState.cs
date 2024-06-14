namespace AeFinder.Grains.State.BlockStates;

public class AppBlockStateChangeState
{
    public BlockStateChange BlockStateChange { get; set; }
}

public class BlockStateChange
{
    public long BlockHeight { get; set; }
    public HashSet<string> ChangeKeys { get; set; }
}