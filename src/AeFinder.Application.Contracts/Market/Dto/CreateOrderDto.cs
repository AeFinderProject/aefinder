using System;
using Orleans;

namespace AeFinder.Market;

[GenerateSerializer]
public class CreateOrderDto
{
    [Id(0)]public string OrganizationId { get; set; }
    [Id(1)]public string UserId { get; set; }
    [Id(2)]public string AppId { get; set; }
    [Id(3)]public string ProductId { get; set; }
    [Id(4)]public int ProductNumber { get; set; }
    [Id(5)]public int PeriodMonths { get; set; }
}