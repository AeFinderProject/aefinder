using System;
using System.Collections.Generic;
using AeFinder.Assets;
using AeFinder.Merchandises;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Orders;

public class OrderIndex : AeFinderDomainEntity<Guid>, IEntityMappingEntity
{
    [Keyword]
    public Guid OrganizationId { get; set; }
    public List<OrderDetailIndex> Details  { get; set; }
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public int Status  { get; set; }
    public int PaymentType  { get; set; }
    public DateTime OrderTime  { get; set; }
    public DateTime PaymentTime { get; set; }
    public Dictionary<string, string> ExtraData { get; set; } = new();
    [Keyword]
    public Guid UserId { get; set; }
    [Keyword]
    public string TransactionId { get; set; }
}

public class OrderDetailIndex
{
    public AssetIndex OriginalAsset { get; set; }
    public MerchandiseIndex Merchandise { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
}