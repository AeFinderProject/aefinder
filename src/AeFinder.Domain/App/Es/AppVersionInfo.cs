using System;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppVersionInfo
{
    [Keyword] public string CurrentVersion { get; set; }
    [Keyword] public string PendingVersion { get; set; }
}