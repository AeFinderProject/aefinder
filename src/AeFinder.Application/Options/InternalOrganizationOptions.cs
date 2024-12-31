using System.Collections.Generic;

namespace AeFinder.Options;

public class InternalOrganizationOptions
{
    public List<string> InternalApps { get; set; }
    public string DefaultFullPodLimitCpuCore { get; set; }
    public string DefaultFullPodLimitMemory { get; set; }
}