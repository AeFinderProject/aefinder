using System;
using Orleans;

namespace AeFinder.Market;

[GenerateSerializer]
public class RenewalDto
{
    [Id(0)]public string SubscriptionId { get; set; }
    [Id(1)]public string OrganizationId { get; set; }
    [Id(2)]public string OrderId { get; set; }
    [Id(3)]public string UserId { get; set; }
    [Id(4)]public string AppId { get; set; }
    [Id(5)]public string ProductId { get; set; }
    [Id(6)]public ProductType ProductType { get; set; }
    [Id(7)]public int ProductNumber { get; set; }
    [Id(8)]public DateTime StartDate { get; set; }
    [Id(9)]public DateTime LastChargeDate { get; set; }
    [Id(10)]public DateTime NextRenewalDate { get; set; }
    [Id(11)]public RenewalPeriod RenewalPeriod { get; set; }
    [Id(12)]public decimal PeriodicCost { get; set; }
    [Id(13)]public bool IsActive { get; set; }
}