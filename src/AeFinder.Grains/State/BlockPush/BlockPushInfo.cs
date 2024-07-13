namespace AeFinder.Grains.State.BlockPush;

[GenerateSerializer]
public class BlockPushInfo
{
    [Id(0)]public string Version { get; set; }
    //public string ChainId { get; set; }
    [Id(1)]public string AppId { get; set; }
    [Id(2)]public string PushToken { get; set; }
}