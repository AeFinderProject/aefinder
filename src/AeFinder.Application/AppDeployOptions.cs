namespace AeFinder;

public class AppDeployOptions
{
    public string AppImageName { get; set; }
    public long MaxAppCodeSize { get; set; } = 12 * 1024 * 1024;
    public long MaxAppAttachmentSize { get; set; } = 120 * 1024 * 1024;
}