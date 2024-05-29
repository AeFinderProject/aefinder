using AeFinder.Apps;

namespace AeFinder.Grains.State.Apps;

public class AppState
{
    public string AppId { get; set; }
    public string OrganizationId { get; set; }
    public string DeployKey { get; set; }
    public string AppName { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public string SourceCodeUrl { get; set; }
    public AppStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}