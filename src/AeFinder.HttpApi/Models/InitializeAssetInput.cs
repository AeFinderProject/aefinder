using System;
using System.Collections.Generic;

namespace AeFinder.Models;

public class InitializeAssetInput
{
    public List<Guid> OrganizationIds { get; set; } = new();
}