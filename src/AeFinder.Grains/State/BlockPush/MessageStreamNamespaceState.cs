namespace AeFinder.Grains.State.BlockPush;

[GenerateSerializer]
public class MessageStreamNamespaceState
{
    [Id(0)]public HashSet<string> AppIds { get; set; } = new();
}