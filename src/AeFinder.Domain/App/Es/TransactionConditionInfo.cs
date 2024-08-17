using System.Collections.Generic;
using Nest;

namespace AeFinder.App.Es;

public class TransactionConditionInfo
{
    [Keyword] public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}