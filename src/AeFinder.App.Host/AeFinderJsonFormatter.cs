using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace AeFinder.App;

public class AeFinderJsonFormatter : ITextFormatter
{
  private readonly JsonValueFormatter _valueFormatter;
  
  private static string _appIdKey = "AppId";
  private static string _versionKey = "Version";
  private static string _eventIdKey = "EventId";
  
  /// <summary>
  /// Construct a <see cref="T:Serilog.Formatting.Compact.CompactJsonFormatter" />, optionally supplying a formatter for
  /// <see cref="T:Serilog.Events.LogEventPropertyValue" />s on the event.
  /// </summary>
  /// <param name="valueFormatter">A value formatter, or null.</param>
  public AeFinderJsonFormatter(JsonValueFormatter valueFormatter = null) =>
    _valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");

  /// <summary>
  /// Format the log event into the output. Subsequent events will be newline-delimited.
  /// </summary>
  /// <param name="logEvent">The event to format.</param>
  /// <param name="output">The output.</param>
  public void Format(LogEvent logEvent, TextWriter output)
  {
    FormatEvent(logEvent, output, _valueFormatter);
    output.WriteLine();
  }

  /// <summary>Format the log event into the output.</summary>
  /// <param name="logEvent">The event to format.</param>
  /// <param name="output">The output.</param>
  /// <param name="valueFormatter">A value formatter for <see cref="T:Serilog.Events.LogEventPropertyValue" />s on the event.</param>
  public static void FormatEvent(
    LogEvent logEvent,
    TextWriter output,
    JsonValueFormatter valueFormatter)
  {
    if (logEvent == null)
      throw new ArgumentNullException(nameof(logEvent));
    if (output == null)
      throw new ArgumentNullException(nameof(output));
    if (valueFormatter == null)
      throw new ArgumentNullException(nameof(valueFormatter));
    output.Write("{\"time\":\"");
    output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
    output.Write("\",\"message\":");
    JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Render(logEvent.Properties), output);
    output.Write(",\"level\":\"");
    output.Write(logEvent.Level);
    output.Write('"');

    output.Write(",\"exception\":");
    JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception == null ? string.Empty : logEvent.Exception.ToString(),
      output);
    
    output.Write(',');
    JsonValueFormatter.WriteQuotedJsonString("appId", output);
    output.Write(':');
    if (logEvent.Properties.TryGetValue(_appIdKey, out var appId))
    {
      valueFormatter.Format(appId, output);
    }
    else
    {
      JsonValueFormatter.WriteQuotedJsonString(string.Empty, output);
    }
    
    output.Write(',');
    JsonValueFormatter.WriteQuotedJsonString("version", output);
    output.Write(':');
    if (logEvent.Properties.TryGetValue(_versionKey, out var version))
    {
      valueFormatter.Format(version, output);
    }
    else
    {
      JsonValueFormatter.WriteQuotedJsonString(string.Empty, output);
    }
    
    output.Write(",\"eventId\":");
    if (logEvent.Properties.TryGetValue(_eventIdKey, out var eventId))
    {
      output.Write(((StructureValue)eventId).Properties[0].Value);
    }
    else
    {
      output.Write(0);
    }

    output.Write('}');
  }
}