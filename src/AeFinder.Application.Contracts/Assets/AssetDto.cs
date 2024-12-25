using System;
using AeFinder.Merchandises;

namespace AeFinder.Assets;

public class AssetDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public MerchandiseDto Merchandise { get; set; }
    public decimal PaidAmount { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public long FreeQuantity { get; set; }
    public long FreeReplicas { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AssetStatus Status { get; set; }
    public string AppId { get; set; }
}