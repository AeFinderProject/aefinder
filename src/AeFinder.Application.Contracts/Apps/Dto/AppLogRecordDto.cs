using System;

namespace AeFinder.Apps.Dto;

public class AppLogRecordDto
{
    public string Log_id { get; set; }
    public DateTime Timestamp { get; set; }

    public string Environment { get; set; }
    
    public AppLogInfo App_log { get; set; }
}