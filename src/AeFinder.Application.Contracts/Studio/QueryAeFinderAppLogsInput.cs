using System;
using System.ComponentModel.DataAnnotations;

namespace AeFinder.Studio;

public class QueryAeFinderAppLogsInput
{
    [Required] public string AppId { get; set; }
    public string Level { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Now.AddMinutes(-5);
    public DateTime EndTime { get; set; } = DateTime.Now;
    public string Pod { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
}