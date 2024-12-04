using System.Collections.Generic;

namespace AeFinder.ApiKeys;

public class SetAuthorisedApiInput
{
    public Dictionary<BasicApi, bool> Apis { get; set; }
}