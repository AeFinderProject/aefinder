using System;
using Orleans;

namespace AeFinder.Market;

[GenerateSerializer]
public class CreateRenewalDto
{
    [Id(0)]public string OrganizationId { get; set; }
    [Id(1)]public string OrderId { get; set; }
    [Id(2)]public string UserId { get; set; }
    [Id(3)]public string AppId { get; set; }
    [Id(4)]public string ProductId { get; set; }
    [Id(5)]public int ProductNumber { get; set; }
    [Id(6)]public RenewalPeriod RenewalPeriod { get; set; }
}