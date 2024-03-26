using AeFinder.Sdk.Entities;

namespace AeFinder.Sdk.Dtos;

public class AeFinderEntityDto
{
    public string Id { get; set; }
    public Metadata Metadata { get; set; } = new ();
}

public class MetadataDto
{
    public string ChainId { get; set; }
    public BlockMetadataDto Block { get; set; }
}

public class BlockMetadataDto
{
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}