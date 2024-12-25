using System;
using System.Collections.Generic;

namespace AeFinder.Orders;

public class CreateOrderInput
{
    public List<CreateOrderDetail> Details  { get; set; }
    public Dictionary<string,string> ExtendedData { get; set; }
}

public class CreateOrderDetail
{
    public Guid? OriginalAssetId { get; set; }
    public Guid MerchandiseId { get; set; }
    public int MerchandiseQuantity { get; set; }
}