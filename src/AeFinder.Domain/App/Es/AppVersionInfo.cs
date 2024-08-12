using System;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppVersionInfo
{
    [Keyword] public string Version { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    [Keyword] public string DockerImage { get; set; }
    public SubscriptionManifestInfo SubscriptionManifest { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
}