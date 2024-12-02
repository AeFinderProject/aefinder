namespace AeFinder.Market;

public class FullPodResourceLevelDto
{
    public string ProductId { get; set; }
    public string LevelName { get; set; }
    public ResourceCapacity Capacity { get; set; }
    public decimal MonthlyUnitPrice { get; set; }
}

public class ResourceCapacity
{
    public string Cpu { get; set; }
    public string Memory { get; set; }
    public string Disk { get; set; }
}