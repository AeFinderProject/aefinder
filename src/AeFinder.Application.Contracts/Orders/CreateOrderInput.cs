using System;
using System.Collections.Generic;
using Orleans;

namespace AeFinder.Orders;

[GenerateSerializer]
public class CreateOrderInput
{
    [Id(0)]public List<CreateOrderDetail> Details  { get; set; }
    [Id(1)]public Dictionary<string,string> ExtraData { get; set; }
}

[GenerateSerializer]
public class CreateOrderDetail
{
    [Id(0)]public Guid? OriginalAssetId { get; set; }
    [Id(1)]public Guid MerchandiseId { get; set; }
    [Id(2)]public long Quantity { get; set; }
    [Id(3)]public long Replicas { get; set; }
}