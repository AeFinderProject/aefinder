using AeFinder.Apps;
using Nest;
using Newtonsoft.Json;

namespace AeFinder.Logger.Entities;

public class AppLogIndex
{
    public string Log_id { get; set; }

    [PropertyName("@timestamp")]
    public DateTime Timestamp { get; set; }

    public string Environment { get; set; }

    public string Message { get; set; }

    // public FileBeatLogHostInfo Host { get; set; }
    //
    // public FileBeatLogAgentInfo Agent { get; set; }

    public AppLogInfo App_log { get; set; }
}