namespace AElfIndexer.Grains.EventData;

[Serializable]
public class BlockStateEventData
{
    public string BlockHash { get; set; }
    public BlockEventData BlockInfo { get; set; }
}