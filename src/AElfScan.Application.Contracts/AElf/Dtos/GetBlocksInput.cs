namespace AElfScan.AElf.Dtos;

public class GetBlocksInput
{
    public long StartBlockNumber { get; set; }
    public long EndBlockNumber { get; set; }
    public bool HasTransaction { get; set; } = false;
}