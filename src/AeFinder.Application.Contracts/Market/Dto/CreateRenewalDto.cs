using System;

namespace AeFinder.Market;

public class CreateRenewalDto
{
    public string OrganizationId { get; set; }
    public string OrderId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string ProductId { get; set; }
    public int ProductNumber { get; set; }
    public RenewalPeriod RenewalPeriod { get; set; }
}