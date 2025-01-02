using System;
using System.Collections.Generic;
using AeFinder.Merchandises;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Billings;

public class BillingIndex : AeFinderDomainEntity<Guid>, IEntityMappingEntity
{
    [Keyword]
    public Guid OrganizationId { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Type { get; set; }
    public List<BillingDetailIndex> Details { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int Status  { get; set; }
    [Keyword]
    public string TransactionId { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime PaymentTime { get; set; }
}

public class BillingDetailIndex
{
    public MerchandiseIndex Merchandise { get; set; }
    [Keyword]
    public string AppId { get; set; }
    public int Quantity { get; set; }
    public int Replicas { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
}