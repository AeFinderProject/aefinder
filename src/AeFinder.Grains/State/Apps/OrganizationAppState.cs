namespace AeFinder.Grains.State.Apps;

public class OrganizationAppState
{
    public string OrganizationId { get; set; }
    public HashSet<string> AppIds { get; set; } = new();
}