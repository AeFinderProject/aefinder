using System;

namespace AeFinder.Apps;

public class AppLogInfo
{
    public string ChainId { get; set; }
    public int EventId { get; set; }
    public DateTime Time { get; set; }
    public string Message { get; set; }
    public string Level { get; set; }
    public string Exception { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
}