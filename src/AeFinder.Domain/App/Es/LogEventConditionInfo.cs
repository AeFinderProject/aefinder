using System.Collections.Generic;
using Nest;

namespace AeFinder.App.Es;

public class LogEventConditionInfo
{
    [Keyword] public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}