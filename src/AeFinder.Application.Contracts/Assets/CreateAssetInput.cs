using System;
using Orleans;

namespace AeFinder.Assets;

[GenerateSerializer]
public class CreateAssetInput
{
    [Id(0)]public Guid MerchandiseId { get; set; }
    [Id(1)]public decimal PaidAmount { get; set; }
    [Id(2)]public long Quantity { get; set; }
    [Id(3)]public long Replicas { get; set; }
    [Id(4)]public long FreeQuantity { get; set; }
    [Id(5)]public long FreeReplicas { get; set; }
    [Id(6)]public AssetFreeType FreeType { get; set; }
    [Id(7)]public DateTime CreateTime { get; set; }
    [Id(8)]public DateTime EndTime { get; set; }
}