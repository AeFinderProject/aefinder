using AeFinder.Apps;
using Nest;

namespace AeFinder.Logger.Entities;

public class AppLogIndex
{
    [Keyword] public string Id { get; set; }

    public DateTime @Timestamp { get; set; }

    public string Environment { get; set; }

    public string Message { get; set; }

    public FileBeatLogHostInfo Host { get; set; }

    public FileBeatLogAgentInfo Agent { get; set; }

    public AppLogInfo App_log { get; set; }
}