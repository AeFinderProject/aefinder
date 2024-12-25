using System.Collections.Generic;

namespace AeFinder.Assets;

public class AppAssetChangedEto
{
    public string AppId { get; set; }
    public List<AssetChangedEto> OriginalAssets { get; set; }
    public List<AssetChangedEto> Assets { get; set; }
}