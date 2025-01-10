using System.Collections.Generic;
using AeFinder.Merchandises;

namespace AeFinder.Assets;

public class AssetInitializationOptions
{
    public List<AssetInitializationItem> Assets { get; set; } = new();
}

public class AssetInitializationItem
{
    public MerchandiseType MerchandiseType { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public long FreeQuantity { get; set; }
    public long FreeReplicas { get; set; }
    public AssetFreeType AssetFreeType { get; set; }
}