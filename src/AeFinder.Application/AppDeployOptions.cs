namespace AeFinder;

public class AppDeployOptions
{
    public string AppImageName { get; set; }
    public long MaxAppCodeSize { get; set; } = 12 * 1024 * 1024;
}