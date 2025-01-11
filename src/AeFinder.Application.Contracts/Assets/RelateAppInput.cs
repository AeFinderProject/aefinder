using System;
using System.Collections.Generic;

namespace AeFinder.Assets;

public class RelateAppInput
{
    public string AppId { get; set; }
    public List<Guid> AssetIds { get; set; }
}