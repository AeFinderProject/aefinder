using Nest;

namespace AeFinder.Logger.Entities;

public class AppLogDetail
{
    [Keyword] public string ChainId { get; set; }
    public int EventId { get; set; }
    public DateTime Time { get; set; }
    [Keyword] public string Message { get; set; }
    [Keyword] public string Level { get; set; }
    [Keyword] public string Exception { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Version { get; set; }
}