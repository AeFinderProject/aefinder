using AElf.Types;

namespace AeFinder.App.TestBase;

public static class LogEventHelper
{
    public static AeFinder.Sdk.Processor.LogEvent ToSdkLogEvent(this LogEvent logEvent)
    {
        var sdkLogEvent = new AeFinder.Sdk.Processor.LogEvent
        {
            ExtraProperties = new Dictionary<string, string>
            {
                {"Indexed", logEvent.Indexed.ToString()},
                {"NonIndexed", logEvent.NonIndexed.ToBase64()}
            }
        };
        return sdkLogEvent;
    }
}