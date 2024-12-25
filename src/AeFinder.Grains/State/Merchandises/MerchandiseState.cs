using AeFinder.Merchandises;

namespace AeFinder.Grains.State.Merchandises;

public class MerchandiseState
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description  { get; set; }
    public string Unit { get; set; }
    public decimal Price { get; set; }
    public ChargeType ChargeType { get; set; }
    public MerchandiseCategory Category { get; set; }
    public MerchandiseType Type { get; set; }
    public MerchandiseStatus Status  { get; set; }
    public int SortWeight { get; set; }
}