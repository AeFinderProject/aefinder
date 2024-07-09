namespace AeFinder.Grains.State.BlockPush;

[GenerateSerializer]
public class BlockPusherManagerState
{
    [Id(0)]public Dictionary<string, HashSet<string>> BlockPusherIds { get; set; } = new();
}