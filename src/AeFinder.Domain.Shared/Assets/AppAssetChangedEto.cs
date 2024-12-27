using System.Collections.Generic;

namespace AeFinder.Assets;

public class AppAssetChangedEto
{
    public List<ChangedAsset> ChangedAssets { get; set; } = new();
    public string AppId { get; set; }
}

public class ChangedAsset
{
    public AssetChangedEto OriginalAsset { get; set; }
    public AssetChangedEto Asset { get; set; }
}