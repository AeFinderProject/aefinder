using System.Collections.Generic;

namespace AeFinder.Options;

public class CustomOrganizationOptions
{
    public List<string> CustomApps { get; set; }
    public string DefaultFullPodLimitCpuCore { get; set; }
    public string DefaultFullPodLimitMemory { get; set; }
}