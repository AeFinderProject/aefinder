using AeFinder.Grains.State.Client;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Newtonsoft.Json;

namespace AeFinder.Client.Helpers;

public  static class AElfLogEventDeserializationHelper
{
    public static T DeserializeAElfLogEvent<T>(LogEventInfo logEventDto) where T : IEvent<T>, new()
    {
        logEventDto.ExtraProperties.TryGetValue("Indexed", out var indexed);
        logEventDto.ExtraProperties.TryGetValue("NonIndexed", out var nonIndexed);

        var indexedList = indexed != null ? JsonConvert.DeserializeObject<List<string>>(indexed) : new List<string>();
        // Log.Debug($"AElfLogEventDeserializationHelper DeserializeAElfLogEvent Indexed:{Indexed}");
        // Log.Debug($"AElfLogEventDeserializationHelper DeserializeAElfLogEvent NonIndexed:{NonIndexed}");
        
        var logEvent = new LogEvent
        {
            Indexed= {indexedList?.Select(ByteString.FromBase64)},
            //NonIndexed =NoIndexed is not null? ByteString.FromBase64(NoIndexed): null
        };
        if (nonIndexed != null)
        {
            logEvent.NonIndexed = ByteString.FromBase64(nonIndexed);
        }
        var message = new T();
        message.MergeFrom(logEvent);
        return message;
    }
}