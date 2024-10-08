namespace AeFinder.Grains.State.Apps;

[GenerateSerializer]
public class OrganizationAppState
{
    [Id(0)]public string OrganizationId { get; set; }
    [Id(1)]public HashSet<string> AppIds { get; set; } = new();
    [Id(2)]public int MaxAppCount { get; set; }
    [Id(3)]public string OrganizationName { get; set; }
}