using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;
using Newtonsoft.Json;

namespace AElfIndexer.Sdk;

public static class LogEventDeserializationHelper
{
    public static T DeserializeLogEvent<T>(LogEvent logEvent) where T : IEvent<T>, new()
    {
        logEvent.ExtraProperties.TryGetValue("Indexed", out var indexed);
        logEvent.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

        var indexedList = indexed != null ? JsonConvert.DeserializeObject<List<string>>(indexed) : new List<string>();

        var @event = new AElf.Types.LogEvent
        {
            Indexed = { indexedList?.Select(ByteString.FromBase64) },
        };
        if (nonIndexed != null)
        {
            @event.NonIndexed = ByteString.FromBase64(nonIndexed);
        }

        var message = new T();
        message.MergeFrom(@event);
        return message;
    }
}