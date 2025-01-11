using System;
using Orleans;

namespace AeFinder.Merchandises;

[GenerateSerializer]
public class MerchandiseDto
{
    [Id(0)]public Guid Id { get; set; }
    [Id(1)]public string Name { get; set; }
    [Id(2)]public string Description  { get; set; }
    [Id(3)]public string Specification { get; set; }
    [Id(4)]public string Unit { get; set; }
    [Id(5)]public decimal Price { get; set; }
    [Id(6)]public ChargeType ChargeType { get; set; }
    [Id(7)]public MerchandiseCategory Category { get; set; }
    [Id(8)]public MerchandiseType Type { get; set; }
    [Id(9)]public MerchandiseStatus Status  { get; set; }
    [Id(10)]public int SortWeight { get; set; }
}