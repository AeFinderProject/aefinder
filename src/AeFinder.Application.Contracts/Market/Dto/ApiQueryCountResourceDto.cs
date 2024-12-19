namespace AeFinder.Market;

public class ApiQueryCountResourceDto
{
    public string ProductId { get; set; }
    public int QueryCount { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
}