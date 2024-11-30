namespace AeFinder.Grains.State.Market;

public class ProductState
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public ProductType ProductType { get; set; }
    public string Description { get; set; }
    public string ProductSpecifications { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
    public bool IsActive { get; set; }
}