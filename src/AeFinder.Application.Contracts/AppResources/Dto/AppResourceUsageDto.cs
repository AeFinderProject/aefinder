using System;
using System.Collections.Generic;
using AeFinder.Apps;

namespace AeFinder.AppResources.Dto;

public class AppResourceUsageDto
{
    public AppInfoImmutable AppInfo { get; set; }
    public Guid OrganizationId { get; set; }
    public Dictionary<string, ResourceUsageDto> ResourceUsages { get; set; }
}

public class ResourceUsageDto
{
    public decimal StoreSize { get; set; }
}