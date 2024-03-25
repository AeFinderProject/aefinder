namespace AeFinder.Grains.State.BlockPush;

public class BlockPusherManagerState
{
    public Dictionary<string, HashSet<string>> BlockPusherIds { get; set; } = new();
}