using System;

namespace AeFinder.ApiKeys;

public class AdjustQueryLimitInput
{
    public Guid OrganizationId { get; set; }
    public long Count { get; set; }
}