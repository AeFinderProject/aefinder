using AeFinder.Assets;

namespace AeFinder.Grains.State.Assets;

[GenerateSerializer]
public class AssetState
{
    [Id(0)]public Guid Id { get; set; }
    [Id(1)]public Guid OrganizationId { get; set; }
    [Id(2)]public Guid MerchandiseId { get; set; }
    [Id(3)]public decimal PaidAmount { get; set; }
    [Id(4)]public long Quantity { get; set; }
    [Id(5)]public long Replicas { get; set; }
    [Id(6)]public long FreeQuantity { get; set; }
    [Id(7)]public long FreeReplicas { get; set; }
    [Id(8)]public AssetFreeType FreeType { get; set; }
    [Id(9)]public DateTime CreateTime { get; set; }
    [Id(10)]public DateTime StartTime { get; set; }
    [Id(11)]public DateTime EndTime { get; set; }
    [Id(12)]public AssetStatus Status { get; set; }
    [Id(13)]public string AppId { get; set; }
    [Id(14)]public bool IsLocked { get; set; }
}