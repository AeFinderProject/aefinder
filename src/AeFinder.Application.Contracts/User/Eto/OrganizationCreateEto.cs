using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.User.Eto;

[EventName("AeFinder.OrganizationCreateEto")]
[GenerateSerializer]
public class OrganizationCreateEto
{
    [Id(0)] public string OrganizationId { get; set; }
    [Id(1)] public string OrganizationName { get; set; }
    [Id(2)] public int MaxAppCount { get; set; }
}