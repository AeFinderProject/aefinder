using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.User.Eto;

[EventName("AeFinder.MaxAppCountUpdateEto")]
[GenerateSerializer]
public class MaxAppCountUpdateEto
{
    [Id(0)] public string OrganizationId { get; set; }
    [Id(1)] public int MaxAppCount { get; set; }
}