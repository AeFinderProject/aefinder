using Orleans;

namespace AeFinder.Merchandises;

[GenerateSerializer]
public class CreateMerchandiseInput
{
    [Id(0)]public string Name { get; set; }
    [Id(1)]public string Description  { get; set; }
    [Id(2)]public string Specification { get; set; }
    [Id(3)]public string Unit { get; set; }
    [Id(4)]public decimal Price { get; set; }
    [Id(5)]public ChargeType ChargeType { get; set; }
    [Id(6)]public MerchandiseCategory Category { get; set; }
    [Id(7)]public MerchandiseType Type { get; set; }
    [Id(8)]public MerchandiseStatus Status  { get; set; }
    [Id(9)]public int SortWeight { get; set; }
}