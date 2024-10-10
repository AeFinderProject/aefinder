using Nest;

namespace AeFinder.App.Es;

public class PodContainerInfo
{
    [Keyword] public string ContainerID { get; set; }
    [Keyword] public string ContainerName { get; set; }
    [Keyword] public string ContainerImage { get; set; }
    public int RestartCount { get; set; }
    public bool Ready { get; set; }
    [Keyword] public string CurrentState { get; set; }
}