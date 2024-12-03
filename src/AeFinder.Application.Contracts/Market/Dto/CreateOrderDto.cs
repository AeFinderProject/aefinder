using System;

namespace AeFinder.Market;

public class CreateOrderDto
{
    public string OrganizationId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string ProductId { get; set; }
    public int ProductNumber { get; set; }
}