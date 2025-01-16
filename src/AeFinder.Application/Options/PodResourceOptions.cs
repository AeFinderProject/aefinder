using System.Collections.Generic;

namespace AeFinder.Options;

public class PodResourceOptions
{
    public List<ResourceInfo> FullPodResourceInfos { get; set; }
}

public class ResourceInfo
{
    public string ResourceName { get; set; }
    public string Cpu { get; set; }
    public string Memory { get; set; }
}