using System;
using AeFinder.Merchandises;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Assets;

public class AssetIndex : AeFinderDomainEntity<Guid>, IEntityMappingEntity
{
    [Keyword] 
    public Guid OrganizationId { get; set; }
    public MerchandiseIndex Merchandise { get; set; }
    public decimal PaidAmount { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public long FreeQuantity { get; set; }
    public long FreeReplicas { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Status { get; set; }
    [Keyword] 
    public string AppId { get; set; }
}