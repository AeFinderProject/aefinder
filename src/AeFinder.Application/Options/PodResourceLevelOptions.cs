using System.Collections.Generic;

namespace AeFinder.Options;

public class PodResourceLevelOptions
{
    public List<ResourceLevelInfo> FullPodResourceLevels { get; set; }
}

public class ResourceLevelInfo
{
    public string ResourceName { get; set; }
    public string LevelName { get; set; }
    public string Cpu { get; set; }
    public string Memory { get; set; }
}