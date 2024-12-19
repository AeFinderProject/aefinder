using Orleans;

namespace AeFinder.Market;

[GenerateSerializer]
public class CreateSubscriptionBillDto
{
    [Id(0)]public string OrganizationId { get; set; }
    [Id(1)]public string OrderId { get; set; }
    [Id(2)]public string SubscriptionId { get; set; }
    [Id(3)]public string UserId { get; set; }
    [Id(4)]public string AppId { get; set; }
    [Id(5)]public string Description { get; set; }
}