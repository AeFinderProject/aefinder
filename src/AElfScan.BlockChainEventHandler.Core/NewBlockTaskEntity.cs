using AElfScan.Etos;
using AElfScan.Grains.EventData;

namespace AElfScan;

public class NewBlockTaskEntity
{
    public NewBlockEto newBlockEto { get; set; }
    public BlockEventData blockEventData { get; set; }
}