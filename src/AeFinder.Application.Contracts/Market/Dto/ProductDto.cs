using Orleans;

namespace AeFinder.Market;

[GenerateSerializer]
public class ProductDto
{
    [Id(0)]public string ProductId { get; set; }
    [Id(1)]public string ProductName { get; set; }
    [Id(2)]public ProductType ProductType { get; set; }
    [Id(3)]public string Description { get; set; }
    [Id(4)]public string ProductSpecifications { get; set; }
    [Id(5)]public decimal MonthlyUnitPrice { get; set; }
    [Id(6)]public bool IsActive { get; set; }
}