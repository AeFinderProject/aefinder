using System;

namespace AeFinder.Apps;

public class AppDto
{
    public string AppId { get; set; }
    public string DeployKey { get; set; }
    public string AppName { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public string SourceCodeUrl { get; set; }
    public AppStatus Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public string TestId { get; set; }
    public AppVersion Versions { get; set; } = new();
}

public class AppVersion
{
    public string CurrentVersion { get; set; }
    public string PendingVersion { get; set; }
}