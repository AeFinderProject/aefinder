using Volo.Abp.EventBus;

namespace AeFinder.Market.Eto;

[EventName("AeFinder.BillCreateEto")]
public class BillCreateEto
{
    public string OrganizationId { get; set; }
    public string BillingId { get; set; }
}